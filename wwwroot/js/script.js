// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    $(window).on('scroll', function () {
      const scrolled = $(this).scrollTop() > 40;
      $('.site-header').toggleClass('is-scrolled', scrolled);
    });
  
    $('.nav-link, .btn[href^="#"]').on('click', function () {
      const navbar = bootstrap.Collapse.getInstance(document.getElementById('mainNavbar'));
      if (navbar) navbar.hide();
    });
  
    $('#contactForm').on('submit', function (event) {
      event.preventDefault();
  
      Swal.fire({
        icon: 'success',
        title: 'Request sent',
        text: 'Thank you. We will contact you soon to plan your Amalfi Coast experience.',
        confirmButtonText: 'Perfect',
        background: '#0d0d0d',
        color: '#f7f2e8',
        confirmButtonColor: '#d6ad61'
      });
  
      this.reset();
    });
  });