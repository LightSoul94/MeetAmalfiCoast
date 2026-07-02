// Rileva la lingua del browser
const browserLang = navigator.language.toLowerCase();
// Imposta la lingua del datepicker e il formato della data in base alla lingua del browser
let datepickerLanguage = "en";
let dateFormat = "mm/dd/yyyy";
// Se la lingua del browser è italiana, usa la localizzazione italiana
if (browserLang.startsWith("it")) {
  datepickerLanguage = "it";
  dateFormat = "dd/mm/yyyy";
}

let selectedDate = new Date();

let currentWeekDays = [];

let appointments = [];

let unsubscribeAppointments = null;

const startHour = 8;
const endHour = 20;
const slotHeight = 60;
const minutesStep = 15;

$(document).ready(function () {

  loadAppointmentsForSelectedWeek();

  $("#prevWeek").on("click", function () {
    selectedDate.setDate(selectedDate.getDate() - (isMobilePlanning() ? 1 : 7));
    loadAppointmentsForSelectedWeek();
  });

  $("#nextWeek").on("click", function () {
    selectedDate.setDate(selectedDate.getDate() + (isMobilePlanning() ? 1 : 7));
    loadAppointmentsForSelectedWeek();
  });

  setInterval(() => {
    renderCurrentTimeLine();
  }, 1000);

});

function renderCalendar() {
  $("#calendarGrid").empty();

  renderTimeColumn();

  if (isMobilePlanning()) {
    const mobileDay = {
      name: selectedDate.toLocaleDateString(browserLang, { weekday: "long" }),
      shortDate: selectedDate.toLocaleDateString(browserLang, {
        day: "2-digit",
        month: "2-digit"
      }),
      isoDate: toIsoDate(selectedDate),
      date: new Date(selectedDate)
    };

    renderDayColumn(mobileDay);
  } else {
    currentWeekDays = getWeekDays(selectedDate);

    currentWeekDays.forEach((dayInfo) => {
      renderDayColumn(dayInfo);
    });
  }

  renderCurrentTimeLine();
}

function renderTimeColumn() {
  const timeColumn = $("<div>").addClass("time-column");

  timeColumn.append($("<div>").addClass("time-header").text("Ora"));

  for (let hour = startHour; hour < endHour; hour++) {
    const time = `${String(hour).padStart(2, "0")}:00`;

    timeColumn.append(
      $("<div>")
        .addClass("time-slot")
        .append(
          $("<span>")
            .addClass("time-label")
            .text(time)
        )
    );
  }

  $("#calendarGrid").append(timeColumn);
}

function loadAppointmentsForSelectedWeek() {
  if (isMobilePlanning()) {
    const isoDate = toIsoDate(selectedDate);

    if (unsubscribeAppointments) {
      unsubscribeAppointments();
    }

    unsubscribeAppointments = PlanningService.listenAppointmentsByRange(
      isoDate,
      isoDate,
      function (items) {
        appointments = items;
        renderCalendar();
      }
    );

    return;
  }

  currentWeekDays = getWeekDays(selectedDate);

  const startIsoDate = currentWeekDays[0].isoDate;
  const endIsoDate = currentWeekDays[6].isoDate;

  if (unsubscribeAppointments) {
    unsubscribeAppointments();
  }

  unsubscribeAppointments = PlanningService.listenAppointmentsByRange(
    startIsoDate,
    endIsoDate,
    function (items) {
      appointments = items;
      renderCalendar();
    }
  );
}

function renderDayColumn(dayInfo) {
  const column = $("<div>").addClass("day-column");

  column.append(
    $("<div>")
      .addClass("day-header")
      .html(`${capitalize(dayInfo.name)}<br><small>${dayInfo.shortDate}</small>`)
  );

  const body = $("<div>").addClass("day-body");

  for (let hour = startHour; hour < endHour; hour++) {
    for (let minute = 0; minute < 60; minute += minutesStep) {
      const time = `${String(hour).padStart(2, "0")}:${String(minute).padStart(
        2,
        "0"
      )}`;

      const slot = $("<div>")
        .addClass("day-slot quarter-slot")
        .attr("data-date", dayInfo.isoDate)
        .attr("data-time", time)
        .append($("<span>").addClass("slot-time").text(time))
        .on("click", function () {
          openCreateAppointmentAlert(dayInfo, time);
        });

      body.append(slot);
    }
  }

  appointments
    .filter((app) => app.isoDate === dayInfo.isoDate)
    .forEach((app) => {
      body.append(createAppointment(app));
    });

  column.append(body);
  $("#calendarGrid").append(column);
}

function openCreateAppointmentAlert(dayInfo, startTime) {
  Swal.fire({
    title: "Nuovo appuntamento",
    html: `
            <div class="appointment-form text-start">

                <div class="appointment-top-bar mb-2">
                    <div class="d-flex justify-content-between align-items-center">
                        <label class="form-label mb-1">Email</label>
                    </div>
                    <div class="d-flex gap-2">
                        <input id="swal-date" class="form-control appointment-date-input" autocomplete="off">
                    </div>
                </div>

                <div class="mb-2">
                    <div class="d-flex justify-content-between align-items-center">
                        <label class="form-label mb-1">Nominativo</label>
                    </div>

                    <div class="d-flex gap-2">
                        <input id="swal-customer" class="form-control" placeholder="Nominativo">
                    </div>
                </div>

                <div class="mb-2">
                    <div class="d-flex justify-content-between align-items-center">
                        <label class="form-label mb-1">Email</label>
                    </div>

                    <div class="d-flex gap-2">
                        <input id="swal-customer-email" class="form-control" placeholder="Email">
                    </div>
                </div>

                <hr>

                <div class="row g-2 align-items-end">
                    <div class="col-6">
                        <label class="form-label mb-1">Ora:</label>
                        <input id="swal-start" type="time" class="form-control" value="${startTime}" step="900">
                    </div>

                    <div class="col-6">
                        <label class="form-label mb-1">Ora fine</label>
                        <input id="swal-end" type="time" class="form-control" value="${addMinutesToTime(startTime, 30)}" step="900">
                    </div>
                </div>

                <div class="row g-2 align-items-end mt-2">
                    <div class="col-5">
                        <label class="form-label mb-1">Giorno</label>
                        <input id="swal-day" class="form-control" value="${capitalize(dayInfo.name)} ${dayInfo.shortDate}" readonly>
                    </div>

                    <div class="col-7">
                        <input id="swal-title" class="form-control" placeholder="Descrizione">
                    </div>
                </div>
                <hr>
            </div>
            `,
    //Versione Venere
    //     html: `
    // <div class="appointment-form text-start">

    //     <div class="appointment-top-bar">
    //         <input id="swal-date" class="form-control appointment-date-input" autocomplete="off">
    //     </div>

    //     <div class="mb-2">
    //         <div class="d-flex justify-content-between align-items-center">
    //             <label class="form-label mb-1">Cliente</label>
    //         </div>

    //         <div class="d-flex gap-2">
    //             <input id="swal-customer" class="form-control" placeholder="Scrivi per cercare un cliente per nome, cellulare ed email">
    //             <button type="button" class="btn btn-success">Nuovo</button>
    //         </div>
    //     </div>

    //     <hr>

    //     <div class="row g-2 align-items-end">
    //         <div class="col-3">
    //             <label class="form-label mb-1">Ora:</label>
    //             <input id="swal-start" type="time" class="form-control" value="${startTime}" step="900">
    //         </div>

    //         <div class="col-9">
    //             <label class="form-label mb-1">Operatore</label>
    //             <select id="swal-day" class="form-select">
    //                 <option value="${day}" selected>${day}</option>
    //             </select>
    //         </div>

    //         <div class="col-3">
    //             <label class="form-label mb-1">Ora fine</label>
    //             <input id="swal-end" type="time" class="form-control" value="${addMinutesToTime(startTime, 30)}" step="900">
    //         </div>

    //         <div class="col-9">
    //             <div class="mb-1">
    //                 <div class="form-check form-check-inline">
    //                     <input class="form-check-input" type="radio" name="appointmentType" id="swal-type-treatment" value="Trattamento" checked>
    //                     <label class="form-check-label fw-bold text-decoration-underline" for="swal-type-treatment">Trattamento</label>
    //                 </div>

    //                 <div class="form-check form-check-inline">
    //                     <input class="form-check-input" type="radio" name="appointmentType" id="swal-type-product" value="Prodotto">
    //                     <label class="form-check-label" for="swal-type-product">Prodotto</label>
    //                 </div>
    //             </div>

    //             <input id="swal-title" class="form-control" placeholder="Seleziona un trattamento">
    //         </div>
    //     </div>

    //     <div class="text-center mt-1">
    //         <button type="button" class="btn btn-link text-success fw-bold p-0">
    //             Aggiungi all'appuntamento
    //         </button>
    //     </div>

    //     <hr>
    // </div>
    // `,
    showCancelButton: true,
    confirmButtonText: "Paga acconto e prenota",
    cancelButtonText: "Annulla",
    didOpen: () => {
      $("#swal-date").datepicker({
        language: datepickerLanguage,
        format: dateFormat,
        autoclose: true,
        todayHighlight: true,
        weekStart: 1
      });

      $("#swal-date").datepicker("setDate", dayInfo.date);
    },
    preConfirm: () => {
      const title = $("#swal-title").val().trim();
      const customer = $("#swal-customer").val().trim();
      const customerEmail = $("#swal-customer-email").val().trim();
      const pickedDate = $("#swal-date").datepicker("getDate");
      const isoDate = toIsoDate(pickedDate);
      const start = $("#swal-start").val();
      const end = $("#swal-end").val();

      if (!isoDate || !title || !customer || !start || !customerEmail || !start || !end) {
        Swal.showValidationMessage("Compila tutti i campi");
        return false;
      }

      if (timeToMinutes(end) <= timeToMinutes(start)) {
        Swal.showValidationMessage(
          "L'orario di fine deve essere successivo all'inizio"
        );
        return false;
      }

      return {
        title,

        customerName: customer,
        customerEmail,
        customerPhone: "",

        pickupAddress: "",
        dropoffAddress: "",

        isoDate,
        start,
        end,

        notes: "",
        status: "confirmed",

        googleEventId: null,
        googleCalendarId: null,

        syncStatus: "pending",
        syncError: null,

        reminderEmailSent: false,
        reminderEmailSentAt: null,

        lastModifiedBy: "website"
      };
    }
  }).then((result) => {
    if (!result.isConfirmed)
      return;

    Swal.fire({
      title: "Creazione prenotazione",
      text: "Attendere qualche secondo.",
      allowOutsideClick: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });

    PlanningService.createCheckoutSession(result.value)
      .then(function (response) {

        if (response && response.success && response.bypassStripe) {
          Swal.fire({
            icon: "success",
            title: "Prenotazione confermata",
            text: "L'appuntamento è stato creato senza pagamento online."
          });

          return;
        }

        if (response && response.success && response.checkoutUrl) {
          window.location.href = response.checkoutUrl;
          return;
        }

        Swal.fire({
          icon: "error",
          title: "Errore",
          text: response?.message || "Impossibile avviare il pagamento."
        });
      })
      .catch(function () {
        Swal.fire({
          icon: "error",
          title: "Errore pagamento",
          text: "Non è stato possibile avviare il pagamento dell'acconto."
        });
      });
  });
}

function createCheckoutSession(appointment) {
  return $.ajax({
    url: "/Planning/CreateCheckoutSession",
    method: "POST",
    contentType: "application/json",
    data: JSON.stringify(appointment)
  });
}

function createAppointment(app) {
  const startMinutes = timeToMinutes(app.start);
  const endMinutes = timeToMinutes(app.end);
  const calendarStartMinutes = startHour * 60;

  const top = ((startMinutes - calendarStartMinutes) / 60) * slotHeight;
  const height = ((endMinutes - startMinutes) / 60) * slotHeight;

  const appointment = $("<div>")
    .addClass("appointment")
    .css({
      top: `${top}px`,
      height: `${height}px`
    })
    .append($("<strong>").text(app.title || "Reserved"))
    .append($("<span>").text("Reserved"))
    .append($("<span>").text(`${app.start} - ${app.end}`));

  appointment.on("click", function (e) {
    e.stopPropagation();

    Swal.fire({
      title: "Slot reserved",
      text: "This time slot is already booked.",
      icon: "info"
    });
  });

  return appointment;
}

function timeToMinutes(time) {
  const parts = time.split(":");
  return parseInt(parts[0]) * 60 + parseInt(parts[1]);
}

function addMinutesToTime(time, minutesToAdd) {
  const total = timeToMinutes(time) + minutesToAdd;

  const hours = Math.floor(total / 60);
  const minutes = total % 60;

  return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(
    2,
    "0"
  )}`;
}

function formatDate(date) {
  return date.toLocaleDateString(browserLang, {
    weekday: "long",
    day: "2-digit",
    month: "long",
    year: "numeric"
  });
}

// Funzione per disegnare la linea del tempo corrente
function renderCurrentTimeLine() {

  $(".current-time-line").remove();

  const now = new Date();

  const currentMinutes =
    now.getHours() * 60 +
    now.getMinutes();

  const calendarStartMinutes = startHour * 60;
  const calendarEndMinutes = endHour * 60;

  if (
    currentMinutes < calendarStartMinutes ||
    currentMinutes > calendarEndMinutes
  ) {
    return;
  }

  const top =
    ((currentMinutes - calendarStartMinutes) / 60) *
    slotHeight;

  const line = $("<div>")
    .addClass("current-time-line")
    .css("top", `${top}px`);

  $(".day-body").append(line);
}

// Funzione per convertire una data in formato ISO (YYYY-MM-DD)
function toIsoDate(date) {
  return date.getFullYear() +
    "-" +
    String(date.getMonth() + 1).padStart(2, "0") +
    "-" +
    String(date.getDate()).padStart(2, "0");
}

// Funzione per ottenere i giorni della settimana a partire da una data
function getWeekDays(date) {
  const monday = new Date(date);
  const day = monday.getDay();

  const diff = day === 0 ? -6 : 1 - day;
  monday.setDate(monday.getDate() + diff);

  const days = [];

  for (let i = 0; i < 7; i++) {
    const d = new Date(monday);
    d.setDate(monday.getDate() + i);

    days.push({
      name: d.toLocaleDateString(browserLang, { weekday: "long" }),
      shortDate: d.toLocaleDateString(browserLang, {
        day: "2-digit",
        month: "2-digit"
      }),
      isoDate: toIsoDate(d),
      date: d
    });
  }

  return days;
}

// Funzione per capitalizzare la prima lettera di una stringa
function capitalize(text) {
  return text.charAt(0).toUpperCase() + text.slice(1);
}

// Funzione per rilevare se l'utente sta visualizzando il planning da un dispositivo mobile
function isMobilePlanning() {
  return window.innerWidth <= 950;
}