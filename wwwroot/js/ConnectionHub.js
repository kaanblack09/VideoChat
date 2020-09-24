'use strict';
const isDebugging = true;
var hubUrl = document.location.pathname + 'ConnectionHub';
var wsconn = new signalR.HubConnectionBuilder().withUrl("/ConnectionHub").build();
    //.withUrl(hubUrl, signalR.HttpTransportType.WebSockets)
    //.configureLogging(signalR.LogLevel.None).build();

var peerConnectionConfig = { "iceServers": [{ "url": "stun:stun.l.google.com:19302" }] };

document.addEventListener("DOMContentLoaded", () => {
    console.log("Ready for Hub");
    initializeSignalR();
});

var webrtcConstraints = {
    video: {
        width: 640,
        height: 480
    }
};
var streamInfo = { applicationName: WOWZA_APPLICATION_NAME, streamName: WOWZA_STREAM_NAME, sessionId: WOWZA_SESSION_ID_EMPTY };

var WOWZA_STREAM_NAME = null, connections = {}, localStream = null;

const attachMediaStream = (e) => {
    //console.log(e);
    console.log("OnPage: called attachMediaStream ");
    var partnerScreen = document.querySelector("#screen");
    var partnerCamera = document.querySelector("#camera");
    if (partnerScreen.srcObject == null) {
        console.log(e.streams[0].getTracks());
        partnerScreen.srcObject = e.streams[0];
    }
    else {
        console.log(e.streams[0].getTracks());
        partnerCamera.srcObject = e.streams[0];
    }
};

const receivedCandidateSignal = (connection, partnerClientId, candidate) => {
    console.log('WebRTC: adding full candidate');
    connection.addIceCandidate(new RTCIceCandidate(candidate), () => console.log("WebRTC: added candidate successfully"), () => console.log("WebRTC: cannot add candidate"));
}

 //Process a newly received SDP signal
const receivedSdpSignal = (connection, partnerClientId, sdp) => {
    console.log('connection: ', connection);
    console.log('sdp', sdp);
    console.log('WebRTC: called receivedSdpSignal');
    console.log('WebRTC: processing sdp signal');
    connection.setRemoteDescription(new RTCSessionDescription(sdp), () => {
        console.log('WebRTC: set Remote Description');
        if (connection.remoteDescription.type == "offer") {
            console.log('WebRTC: remote Description type offer');
            connection.createAnswer().then((desc) => {
                console.log('WebRTC: create Answer...');
                connection.setLocalDescription(desc, () => {
                    console.log('WebRTC: set Local Description...');
                    console.log('connection.localDescription: ', connection.localDescription);
                    //setTimeout(() => {
                    sendHubSignal(JSON.stringify({ "sdp": connection.localDescription }), partnerClientId);
                    //}, 1000);
                }, errorHandler);
            }, errorHandler);
        } else if (connection.remoteDescription.type == "answer") {
            console.log('WebRTC: remote Description type answer');
        }
    }, errorHandler);
}

// Hand off a new signal from the signaler to the connection
const newSignal = (partnerClientId, data) => {
    console.log('WebRTC: called newSignal');
    //console.log('connections: ', connections);

    var signal = JSON.parse(data);
    var connection = getConnection(partnerClientId);
    console.log("connection: ", connection);

    // Route signal based on type
    if (signal.sdp) {
        console.log('WebRTC: sdp signal');
        receivedSdpSignal(connection, partnerClientId, signal.sdp);
    } else if (signal.candidate) {
        console.log('WebRTC: candidate signal');
        receivedCandidateSignal(connection, partnerClientId, signal.candidate);
    } else {
        console.log('WebRTC: adding null candidate');
        connection.addIceCandidate(null, () => console.log("WebRTC: added null candidate successfully"), () => console.log("WebRTC: cannot add null candidate"));
    }
}

//const onStreamRemoved = (connection, streamId) => {
//    console.log("WebRTC: onStreamRemoved -> Removing stream: ");
//}
// Close the connection between myself and the given partner
const closeConnection = (partnerClientId) => {
    console.log("WebRTC: called closeConnection ");
    var connection = connections[partnerClientId];

    if (connection) {
        connection.close();
        delete connections[partnerClientId]; // Remove the property
    }
}
// Close all of our connections
const closeAllConnections = () => {
    console.log("WebRTC: call closeAllConnections ");
    for (var connectionID in connections) {
        closeConnection(connectionID);
    }
}

const getConnection = (partnerClientId) => {
    console.log("WebRTC: called getConnection");
    if (connections[partnerClientId]) {
        console.log("WebRTC: connections partner client exist");
        return connections[partnerClientId];
    }
    else {
        console.log("WebRTC: initialize new connection");
        return initializeConnection(partnerClientId)
    }
}

const initiateOffer = (partnerClientId) => {
    console.log('WebRTC: called initiateoffer: ');
    var connection = getConnection(partnerClientId); // // get a connection for the given partner
    //console.log('initiate Offer stream: ', stream);
    console.log("offer connection: ", connection);
    connection.addTrack(localscreen.getTracks()[0], localcamera);
    connection.addTrack(localcamera.getTracks()[0], localscreen);
    //connection.(stream);// add our audio/video stream
    console.log("WebRTC: Added local stream");

    connection.createOffer().then(offer => {
        console.log('WebRTC: created Offer: ');
        console.log('WebRTC: Description after offer: ', offer);
        connection.setLocalDescription(offer).then(() => {
            console.log('WebRTC: set Local Description: ');
            console.log('connection before sending offer ', connection);
            setTimeout(() => {
                sendHubSignal(JSON.stringify({ "sdp": connection.localDescription }), partnerClientId);
            }, 1000);
        }).catch(err => console.error('WebRTC: Error while setting local description', err));
    }).catch(err => console.error('WebRTC: Error while creating offer', err));
}
let localscreen;
let localcamera;
const callbackDisplayMediaSuccess = (stream) => {
    console.log("WebRTC: got media stream");
    var screen = document.querySelector("#screen");
    screen.srcObject = stream;
    localscreen = stream;
};
const callbackUserMediaSuccess = (stream) => {
    console.log("WebRTC: got media stream");
    var video = document.querySelector("#camera");
    video.srcObject = stream;
    localcamera = stream;
}
const initializeUserMedia = () => {
    console.log('WebRTC: InitializeUserMedia: ');
    navigator.mediaDevices.getDisplayMedia(webrtcConstraints)
        .then((stream) => callbackDisplayMediaSuccess(stream))
        .catch(err => console.log(err));
    navigator.mediaDevices.getUserMedia({ video: true })
        .then((stream) => callbackUserMediaSuccess(stream))
        .catch(err => console.log(err));
}
// stream removed
const callbackRemoveStream = (connection, evt) => {
    console.log('WebRTC: removing remote stream from partner window');
    // Clear out the partner window
    var otherAudio = document.querySelector('.audio.partner');
    otherAudio.src = '';
}

const callbackAddStream = (connection, evt) => {
    console.log('WebRTC: called callbackAddStream');

    attachMediaStream( evt);
}

//const callbackNegotiationNeeded = (connection, evt) => {
//    console.log("WebRTC: Negotiation needed...");
//    //console.log("Event: ", evt);
//}

const callbackIceCandidate = (evt, connection, partnerClientId) => {
    console.log("WebRTC: Ice Candidate callback");
    //console.log("evt.candidate: ", evt.candidate);
    if (evt.candidate) {// Found a new candidate
        console.log('WebRTC: new ICE candidate');
        //console.log("evt.candidate: ", evt.candidate);
        sendHubSignal(JSON.stringify({ "candidate": evt.candidate }), partnerClientId);
    } else {
        // Null candidate means we are done collecting candidates.
        console.log('WebRTC: ICE candidate gathering complete');
        sendHubSignal(JSON.stringify({ "candidate": null }), partnerClientId);
    }
}

const initializeConnection = (partnerClientId) => {
    console.log('WebRTC: Initializing connection...');
    //console.log("Received Param for connection: ", partnerClientId);

    var connection = new RTCPeerConnection(peerConnectionConfig);
    connection.onicecandidate = evt => callbackIceCandidate(evt, connection, partnerClientId); // ICE Candidate Callback
    //connection.onnegotiationneeded = evt => callbackNegotiationNeeded(connection, evt); // Negotiation Needed Callback
    connection.ontrack = evt => callbackAddStream(connection, evt); // Add stream handler callback
    connection.onremovestream = evt => callbackRemoveStream(connection, evt); // Remove stream handler callback
    connections[partnerClientId] = connection; // Store away the connection based on username
    return connection;
}

const sendHubSignal = (candidate, partnerClientId) => {
    console.log('candidate', candidate);
    console.log('SignalR: called sendhubsignal ');
    wsconn.invoke('sendSignal', candidate, partnerClientId).catch(errorHandler);
};

wsconn.onclose(e => {
    if (e) {
        console.log("SignalR: closed with error.");
        console.log(e);
    }
    else {
        console.log("Disconnected");
    }
});
// Hub Callback: Update User List
wsconn.on('updateUserList', (userList) => {
    console.log("update list users " + JSON.stringify(userList));
    var listString = "";
    for (var index = 0; index < userList.length; index++) {
        listString += '<li class="list-group-item user" data-cid=' + userList[index].connectionID + ' data-username=' + userList[index].userName + '>' + userList[index].userName + '</li>';
    }
    document.querySelector('#usersdata').innerHTML = listString;
});

// Hub Callback: Call Accepted
wsconn.on('callAccepted', (acceptingUser) => {
    console.log('SignalR: call accepted from: ' + JSON.stringify(acceptingUser) + '.  Initiating WebRTC call and offering my stream up...');

    // Callee accepted our call, let's send them an offer with our video stream
    initiateOffer(acceptingUser.connectionID); // Will use driver email in production
    // Set UI into call mode
});

// Hub Callback: Call Declined
wsconn.on('callDeclined', (decliningUser, reason) => {
    console.log('SignalR: call declined from: ' + decliningUser.connectionID);
    console.log(reason);
});

// Hub Callback: Incoming Call
wsconn.on('incomingCall', (callingUser) => {
    console.log('SignalR: incoming call from: ' + JSON.stringify(callingUser));
    wsconn.invoke('AnswerCall', true, callingUser).catch(err => console.log(err));
});

// Hub Callback: WebRTC Signal Received
wsconn.on('receiveSignal', (signalingUser, signal) => {
    console.log('WebRTC: receive signal ');
    newSignal(signalingUser.connectionID, signal);
});

// Hub Callback: Call Ended
wsconn.on('callEnded', (signalingUser, signal) => {
    console.log('SignalR: call with ' + signalingUser.connectionID + ' has ended: ' + signal);

    // Let the user know why the server says the call is over
    console.log(signal);

    // Close the WebRTC connection
    closeConnection(signalingUser.connectionID);
});

const initializeSignalR = () => {
    console.log("Start connection for hub!")
    wsconn.start()
        .then(() => {
            wsconn.invoke("Join", username,classid)
                .catch((err) => {
                console.log(err);
                console.log("Join Fail !!!")
                });
        })
        .catch(err => console.log(err));
};

document.querySelector("#start").addEventListener('click', () => {
    initializeUserMedia();
});
document.querySelector("#call").addEventListener('click', () => {
    console.log("Start call broadcast.");
    wsconn.invoke("CallUser").catch(err => console.log(err));
});
document.querySelector("#hangUp").addEventListener("click", () => {
    console.log("Hang up !");
    wsconn.invoke('hangUp');
    closeAllConnections();
});


const errorHandler = (error) => {
    if (error.message)
        console.log('Error Occurred - Error Info: ' + JSON.stringify(error.message));
    else
        console.log('Error Occurred - Error Info: ' + JSON.stringify(error));

    consoleLogger(error);
};

const consoleLogger = (val) => {
    if (isDebugging) {
        console.log(val);
    }
};
