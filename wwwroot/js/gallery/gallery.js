$(function () {
    const galleryItems = $(".gallery-item");
    const lightbox = $("#galleryLightbox");
    const lightboxImage = $("#galleryLightboxImage");
    const lightboxCaption = $("#galleryLightboxCaption");

    let currentImageIndex = 0;

    function openGalleryImage(index) {
        const item = galleryItems.eq(index);
        const imageUrl = item.data("gallery-image");
        const imageAlt = item.data("gallery-alt") || "";

        currentImageIndex = index;

        lightboxImage.attr("src", imageUrl);
        lightboxImage.attr("alt", imageAlt);
        lightboxCaption.text(imageAlt);

        lightbox
            .addClass("is-open")
            .attr("aria-hidden", "false");

        $("body").addClass("gallery-lightbox-open");

        $("#closeGalleryLightbox").trigger("focus");
    }

    function closeGalleryLightbox() {
        lightbox
            .removeClass("is-open")
            .attr("aria-hidden", "true");

        $("body").removeClass("gallery-lightbox-open");

        lightboxImage.attr("src", "");

        galleryItems.eq(currentImageIndex).trigger("focus");
    }

    function showPreviousImage() {
        currentImageIndex--;

        if (currentImageIndex < 0) {
            currentImageIndex = galleryItems.length - 1;
        }

        openGalleryImage(currentImageIndex);
    }

    function showNextImage() {
        currentImageIndex++;

        if (currentImageIndex >= galleryItems.length) {
            currentImageIndex = 0;
        }

        openGalleryImage(currentImageIndex);
    }

    galleryItems.on("click", function () {
        const index = galleryItems.index(this);
        openGalleryImage(index);
    });

    $("#closeGalleryLightbox").on("click", function () {
        closeGalleryLightbox();
    });

    $("#previousGalleryImage").on("click", function () {
        showPreviousImage();
    });

    $("#nextGalleryImage").on("click", function () {
        showNextImage();
    });

    lightbox.on("click", function (event) {
        if (event.target === this) {
            closeGalleryLightbox();
        }
    });

    $(document).on("keydown", function (event) {
        if (!lightbox.hasClass("is-open")) {
            return;
        }

        if (event.key === "Escape") {
            closeGalleryLightbox();
        }

        if (event.key === "ArrowLeft") {
            showPreviousImage();
        }

        if (event.key === "ArrowRight") {
            showNextImage();
        }
    });
});