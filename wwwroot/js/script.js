// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Metodo per la gestione dello scroll della navbar e del form di contatto
$(function () {

  function closeMobileNavbar() {
    const navbarElement = document.getElementById("mainNavbar");

    if (!navbarElement)
      return;

    const navbar = bootstrap.Collapse.getInstance(navbarElement);

    if (navbar)
      navbar.hide();
  }

  function setActiveNavByHash(hash) {
    $(".nav-link").removeClass("active");

    if (!hash || hash === "#home") {
      $('.nav-link[href="/"], .nav-link[href="/#home"]').addClass("active");
      return;
    }

    $(`.nav-link[href="/${hash}"], .nav-link[href="${hash}"]`).addClass("active");
  }

  function updateActiveSection() {
    if (window.location.pathname !== "/")
      return;

    const headerHeight = $(".site-header").outerHeight() || 0;
    const scrollPosition = $(window).scrollTop() + headerHeight + 120;

    let activeId = "";

    $("section[id]").each(function () {
      const sectionTop = $(this).offset().top;
      const sectionBottom = sectionTop + $(this).outerHeight();

      if (scrollPosition >= sectionTop && scrollPosition < sectionBottom) {
        activeId = $(this).attr("id");
      }
    });

    if (activeId) {
      setActiveNavByHash("#" + activeId);
    } else {
      setActiveNavByHash("");
    }
  }

  function scrollToHashSection(hash) {
    if (!hash)
      return;

    const target = $(hash);

    if (!target.length) {
      window.location.href = "/" + hash;
      return;
    }

    const headerHeight = $(".site-header").outerHeight() || 0;

    $("html, body").animate({
      scrollTop: target.offset().top - headerHeight
    }, 400, function () {
      updateActiveSection();
    });

    setActiveNavByHash(hash);
  }

  $(window).on("scroll", function () {
    const scrolled = $(this).scrollTop() > 40;

    $(".site-header").toggleClass("is-scrolled", scrolled);

    updateActiveSection();
  });

  $(".nav-link[href^='/#'], .nav-link[href^='#'], .btn[href^='#']").on("click", function (event) {
    const hash = this.hash;

    closeMobileNavbar();

    if (!hash)
      return;

    if (window.location.pathname !== "/") {
      window.location.href = "/" + hash;
      return;
    }

    if ($(hash).length) {
      event.preventDefault();

      history.pushState(null, "", hash);
      scrollToHashSection(hash);
    }
  });

  if (window.location.hash) {
    setTimeout(function () {
      scrollToHashSection(window.location.hash);
    }, 100);
  } else if (window.location.pathname === "/") {
    updateActiveSection();
  }

  $("#contactForm").on("submit", function (event) {
    event.preventDefault();

    Swal.fire({
      icon: "success",
      title: "Request sent",
      text: "Thank you. We will contact you soon to plan your Amalfi Coast experience.",
      confirmButtonText: "Perfect",
      background: "#0d0d0d",
      color: "#f7f2e8",
      confirmButtonColor: "#d6ad61"
    });

    this.reset();
  });

});

// Metodo per lo slider dell'hero
$(document).ready(function () {
  initHeroCarousel();
});

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


// Metodo per invio del form di contatto con SweetAlert2 e Ajax
$("#btnSendRequest").on("click", function () {

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

// Metodo per caricare le recensioni di Google
function loadGoogleReviewsSummary() {
  $.get("/Home/GetGoogleReviews", function (data) {
    const rating = data.rating || 0;
    const reviewsCount = data.userRatingCount || 0;

    $("#googleRating").text(rating.toFixed(1));
    $("#googleReviewsCount").text(`Based on ${reviewsCount} Google Reviews`);

    let stars = "";

    for (let i = 1; i <= 5; i++) {
      if (i <= Math.round(rating))
        stars += '<i class="fa-solid fa-star"></i>';
      else
        stars += '<i class="fa-regular fa-star"></i>';
    }

    $("#googleStars").html(stars);
  });
}

$(document).ready(function () {
  loadGoogleReviewsSummary();
});