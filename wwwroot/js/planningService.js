const PlanningService = (function () {
  // Servizio per gestire gli appuntamenti in Firestore
  
  // Ascolta gli appuntamenti in un intervallo di date e chiama il callback con i risultati
  function listenAppointmentsByRange(startIsoDate, endIsoDate, callback) {
    return window.db.collection("appointments")
      .where("isoDate", ">=", startIsoDate)
      .where("isoDate", "<=", endIsoDate)
      .onSnapshot(function (snapshot) {
        const items = [];

        snapshot.forEach(function (doc) {
          items.push({
            id: doc.id,
            ...doc.data()
          });
        });

        callback(items);
      });
  }

  // Crea un nuovo appuntamento in Firestore con stato di sincronizzazione "pending"
  function createAppointment(appointment) {
  return window.db.collection("appointments").add({
    ...appointment,
    createdAt: firebase.firestore.FieldValue.serverTimestamp(),
    updatedAt: firebase.firestore.FieldValue.serverTimestamp(),
    lastModifiedAt: firebase.firestore.FieldValue.serverTimestamp()
  });
}

  // Elimina un appuntamento esistente per id
  function deleteAppointment(id) {
    return window.db.collection("appointments").doc(id).delete();
  }

  // Richiede il backend per sincronizzare gli appuntamenti con Google Calendar
  function syncWithGoogleCalendar() {
    return $.ajax({
      url: "/Planning/SyncGoogleCalendar",
      method: "POST"
    });
  }

  // Reindirizza l'utente al flusso di connessione di Google Calendar
  function connectGoogleCalendar() {
    window.location.href = "/Planning/ConnectGoogleCalendar";
  }

  return {
    listenAppointmentsByRange,
    createAppointment,
    deleteAppointment,
    syncWithGoogleCalendar,
    connectGoogleCalendar
  };

})();