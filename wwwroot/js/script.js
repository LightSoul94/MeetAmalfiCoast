// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Metodo per lo slider dell'hero
$(document).ready(function () {
  initHeroCarousel();
  initDestinationsSlider();
});

// Funzione per lo slider dell'hero
function initHeroCarousel() {
  const slides = $(".hero-slide");

  if (slides.length <= 1)
    return;

  let currentIndex = 0;

  function showNextSlide() {
    const currentSlide = slides.eq(currentIndex);
    const currentVideo = currentSlide.find("video").get(0);

    currentSlide.removeClass("active");

    if (currentVideo) {
      currentVideo.pause();
      currentVideo.currentTime = 0;
    }

    currentIndex++;

    if (currentIndex >= slides.length)
      currentIndex = 0;

    const nextSlide = slides.eq(currentIndex);
    const nextVideo = nextSlide.find("video").get(0);

    nextSlide.addClass("active");

    if (nextVideo) {
      nextVideo.currentTime = 0;
      nextVideo.play();

      nextVideo.onended = function () {
        showNextSlide();
      };
    } else {
      setTimeout(showNextSlide, 6000);
    }
  }

  const firstVideo = slides.eq(0).find("video").get(0);

  if (firstVideo) {
    firstVideo.play();

    firstVideo.onended = function () {
      showNextSlide();
    };
  } else {
    setTimeout(showNextSlide, 6000);
  }
}

// Funzione per lo slider delle destinazioni
function initDestinationsSlider() {
  const slider = $("#destinationsSlider");

  if (!slider.length)
    return;

  const scrollAmount = () => {
    const firstCard = slider.find(".destination-card").first();

    if (!firstCard.length)
      return 300;

    return firstCard.outerWidth(true) * 2;
  };

  $(".destination-arrow-left").on("click", function () {
    slider.get(0).scrollBy({
      left: -scrollAmount(),
      behavior: "smooth"
    });
  });

  $(".destination-arrow-right").on("click", function () {
    slider.get(0).scrollBy({
      left: scrollAmount(),
      behavior: "smooth"
    });
  });

  let isDragging = false;
  let startX = 0;
  let initialScrollLeft = 0;

  slider.on("mousedown", function (event) {
    isDragging = true;
    startX = event.pageX;
    initialScrollLeft = this.scrollLeft;

    slider.addClass("is-dragging");
  });

  $(document).on("mouseup", function () {
    isDragging = false;
    slider.removeClass("is-dragging");
  });

  slider.on("mouseleave", function () {
    isDragging = false;
    slider.removeClass("is-dragging");
  });

  slider.on("mousemove", function (event) {
    if (!isDragging)
      return;

    event.preventDefault();

    const distance = event.pageX - startX;
    this.scrollLeft = initialScrollLeft - distance;
  });

  slider.on("dragstart", function (event) {
    event.preventDefault();
  });
}

// Metodo per invio del form di contatto con SweetAlert2 e Ajax
$("#btnSendRequest").on("click", function () {

  const button = $("#btnSendRequest");
  button.prop("disabled", true);
  button.text("Sending...");

  const data = {
    Name: $("#name").val().trim(),
    Email: $("#email").val().trim(),
    Service: $("#service").val(),
    Message: $("#message").val().trim()
  };

  if (!data.Name || !data.Email || !data.Service || !data.Message) {

    Swal.fire({
      icon: "warning",
      title: "Missing information",
      text: "Please fill in all fields."
    });

    return;
  }

  $.ajax({

    url: "/Home/SendContactRequest",
    type: "POST",
    contentType: "application/json",
    data: JSON.stringify(data),

    success: function (response) {

      if (response.success) {

        Swal.fire({
          icon: "success",
          title: "Request sent",
          text: "Thank you! We will contact you soon."
        });

        $("#name").val("");
        $("#email").val("");
        $("#service").val("");
        $("#message").val("");
      }
      else {

        Swal.fire({
          icon: "error",
          title: "Error",
          text: response.message
        });
      }
    },

    error: function () {

      Swal.fire({
        icon: "error",
        title: "Server error",
        text: "Unable to send your request."
      });
    }
  });

});