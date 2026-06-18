const firebaseConfig = {
  apiKey: "...",
  authDomain: "...",
  projectId: "test-909e7"
};

firebase.initializeApp(firebaseConfig);

window.db = firebase.firestore();