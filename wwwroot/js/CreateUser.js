
'use strict';
var wsconn = new signalR.HubConnectionBuilder().withUrl("/ConnectionHub").build();

document.addEventListener("DOMContentLoaded", () => {
    
    initialize();
});

const initialize = () => {
    wsconn.start()
        .then(() => {
            
        })
        .catch(err => console.log(err));
}

const makeRandomUser = () => {
    let username = 'User ' + Math.floor((Math.random() * 10000) + 1);
    setUserName(username);
}
const setUserName = (username) => {
    console.log('SingnalR: setting username - ' + username);
    wsconn.invoke("Join", username).catch((err) => {
        console.log(err);
        console.log("Join Fail !!!")
    });
}