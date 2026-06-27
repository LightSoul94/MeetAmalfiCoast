const PlanningService = (function () {

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
      createdAt: firebase.firestore.FieldValue.serverTimestamp()
    });
  }

  return {
    listenAppointmentsByRange,
    createAppointment,
    createCheckoutSession
  };

})();


function createCheckoutSession(appointment) {
    return $.ajax({
      url: "/Planning/CreateCheckoutSession",
      method: "POST",
      contentType: "application/json",
      data: JSON.stringify(appointment)
    });
  }