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

const people = ["Mario", "Luigi", "Anna", "Francesca"];

let appointments = [
  {
    person: "Mario",
    title: "Taglio capelli",
    start: "09:00",
    end: "10:00",
    customer: "Cliente A"
  }
];
 
const startHour = 8;
const endHour = 20;
const slotHeight = 60;
const minutesStep = 15;

$(document).ready(function () {
  renderCalendar();

  $("#prevDay").on("click", function () {
    selectedDate.setDate(selectedDate.getDate() - 1);
    renderCalendar();
  });

  $("#nextDay").on("click", function () {
    selectedDate.setDate(selectedDate.getDate() + 1);
    renderCalendar();
  });

  setInterval(() => {
    renderCurrentTimeLine();
  }, 1000);
});

function renderCalendar() {
  $("#calendarGrid").empty();
  $("#currentDate").text(formatDate(selectedDate));

  renderTimeColumn();

  people.forEach((person) => {
    renderPersonColumn(person);
  });
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

function renderPersonColumn(person) {
  const column = $("<div>").addClass("person-column");

  column.append($("<div>").addClass("person-header").text(person));

  const body = $("<div>").addClass("person-body");

  for (let hour = startHour; hour < endHour; hour++) {
    for (let minute = 0; minute < 60; minute += minutesStep) {
      const time = `${String(hour).padStart(2, "0")}:${String(minute).padStart(
        2,
        "0"
      )}`;

      const slot = $("<div>")
        .addClass("person-slot quarter-slot")
        .attr("data-person", person)
        .attr("data-time", time)
        .append($("<span>").addClass("slot-time").text(time))
        .on("click", function () {
          openCreateAppointmentAlert(person, time);
        });

      body.append(slot);
    }
  }

  appointments
    .filter((app) => app.person === person)
    .forEach((app) => {
      body.append(createAppointment(app));
    });

  column.append(body);
  $("#calendarGrid").append(column);
}

function openCreateAppointmentAlert(person, startTime) {
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
                    <div class="col-3">
                        <label class="form-label mb-1">Operatore</label>
                        <select id="swal-person" class="form-select">
                            <option value="${person}" selected>${person}</option>
                        </select>
                    </div>

                    <div class="col-9">
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
    //             <select id="swal-person" class="form-select">
    //                 <option value="${person}" selected>${person}</option>
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
    confirmButtonText: "Crea appuntamento",
    cancelButtonText: "Annulla",
    didOpen: () => {
      $("#swal-date").datepicker({
        language: datepickerLanguage,
        format: dateFormat,
        autoclose: true,
        todayHighlight: true,
        weekStart: 1
      });

      $("#swal-date").datepicker("setDate", selectedDate);
    },
    preConfirm: () => {
      const title = $("#swal-title").val().trim();
      const customer = $("#swal-customer").val().trim();
      const customerEmail = $("#swal-customer-email").val().trim();
      const pickedDate = $("#swal-date").datepicker("getDate");
      const isoDate = pickedDate.getFullYear() + "-" + String(pickedDate.getMonth() + 1).padStart(2, "0") + "-" + String(pickedDate.getDate()).padStart(2, "0");
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
        isoDate,
        start,
        end,
        title,
        customer,
        person,
        customerEmail
      };
    }
  }).then((result) => {
    if (result.isConfirmed) {
      appointments.push(result.value);
      renderCalendar();

      Swal.fire({
        icon: "success",
        title: "Appuntamento creato",
        timer: 1200,
        showConfirmButton: false
      });
    }
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
    .append($("<strong>").text(app.title))
    .append($("<span>").text(app.customer))
    .append($("<span>").text(`${app.start} - ${app.end}`));

  appointment.on("click", function (e) {
    e.stopPropagation();

    Swal.fire({
      title: app.title,
      html: `
                <strong>Cliente:</strong> ${app.customer}<br>
                <strong>Persona:</strong> ${app.person}<br>
                <strong>Orario:</strong> ${app.start} - ${app.end}
            `,
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

  $(".person-body").append(line);
}