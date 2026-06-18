const firebaseConfig = {
    apiKey: "AIzaSyBHusQ0OEg2u52vebHV3PudaUV3zNtO87Q",
    authDomain: "test-909e7.firebaseapp.com",
    projectId: "test-909e7",
    storageBucket: "test-909e7.firebasestorage.app",
    messagingSenderId: "546623866516",
    appId: "1:546623866516:web:6b96d7a06460dec28b5681",
    measurementId: "G-V1YCNTFH1V"
};

firebase.initializeApp(firebaseConfig);

window.db = firebase.firestore();