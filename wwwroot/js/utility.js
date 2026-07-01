// Metodo per gestione banner cookie
$(function () {
  const cookieConsentKey = "meetAmalfiCoastCookieConsent";
  const cookieBanner = $("#cookieBanner");

  const savedConsent = localStorage.getItem(cookieConsentKey);

  if (!savedConsent) {
    cookieBanner.removeClass("d-none");
  }

  $("#acceptCookies").on("click", function () {
    localStorage.setItem(cookieConsentKey, "accepted");
    cookieBanner.fadeOut(250);
  });

  $("#rejectCookies").on("click", function () {
    localStorage.setItem(cookieConsentKey, "rejected");
    cookieBanner.fadeOut(250);
  });
});