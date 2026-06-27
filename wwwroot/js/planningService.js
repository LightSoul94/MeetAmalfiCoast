const PlanningService = (function () {
  // Servizio per gestire gli appuntamenti in Firestore

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

  function createAppointment(appointment) {
    return window.db.collection("appointments").add({
      ...appointment,
      createdAt: firebase.firestore.FieldValue.serverTimestamp(),
      updatedAt: firebase.firestore.FieldValue.serverTimestamp(),
      lastModifiedAt: firebase.firestore.FieldValue.serverTimestamp()
    });
  }

  function deleteAppointment(id) {
    return window.db.collection("appointments").doc(id).delete();
  }

  function syncWithGoogleCalendar() {
    return $.ajax({
      url: "/Planning/SyncGoogleCalendar",
      method: "POST"
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

  function connectGoogleCalendar() {
    window.location.href = "/Planning/ConnectGoogleCalendar";
  }

  return {
    listenAppointmentsByRange,
    createAppointment,
    deleteAppointment,
    syncWithGoogleCalendar,
    createCheckoutSession,
    connectGoogleCalendar
  };

})();