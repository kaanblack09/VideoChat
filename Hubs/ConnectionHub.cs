using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using VideoChat.Models;

namespace VideoChat.Hubs
{
    public class ConnectionHub : Hub<IConnectionHub>
    {
        private readonly List<IdentityUser> _Users;
        private readonly List<Room> _Rooms;
        //private readonly List<CallOffer> _CallOffers;
        //private List<User> guess = new List<User>();
        public ConnectionHub(List<IdentityUser> users, List<Room> rooms)
        {
            _Users = users;
            _Rooms = rooms;
        }

        public async Task Join(string username,string classid)
        {
            // Add the new user
            IdentityUser usr = new IdentityUser { UserName = username, ConnectionID = Context.ConnectionId };
            _Users.Add(usr);
            ClassRoom clr = new VideoChatDBContext().ClassRoom.Where(u => u.ClassID == classid).SingleOrDefault();
            Room rm = GetRoomByClassID(clr);
            if (rm == null)
            {
                _Rooms.Add(new Room
                {
                    RoomIF = clr,
                    UserCall = new List<IdentityUser> { usr }
                });
                await SendUserListUpdate(GetRoomByClassID(clr));
            }
            else
            {
                rm.UserCall.Add(usr);
                await SendUserListUpdate(rm);
            }
            // Send down the new list to all clients
            
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Room callingRoom = GetRoomByConnectionID(Context.ConnectionId);
            // Hang up any calls the user is in
            await HangUp(); // Gets the user from "Context" which is available in the whole hub

            // Remove the user
            callingRoom.UserCall.RemoveAll(u => u.ConnectionID == Context.ConnectionId);

            // Send down the new user list to all clients
            await SendUserListUpdate(callingRoom);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task CallUser()
        {
            Room callingRoom = GetRoomByConnectionID(Context.ConnectionId);
            IdentityUser callingUser = callingRoom.UserCall.SingleOrDefault(u => u.ConnectionID == Context.ConnectionId);
            var targetUsers = new List<IdentityUser>();
            callingRoom.UserCall.ForEach(u => {
                if(u != callingUser)
                    targetUsers.Add(u);
            });
            // Make sure the person we are trying to call is still here
            foreach (IdentityUser u in targetUsers)
            {
                if (u == null)
                {
                    // If not, let the caller know
                    await Clients.Caller.CallDeclined(u, "The user you called has left.");
                    return;
                }
            }
            //set user make room is 
            callingUser.IsCaller = true;
            // They are here, so tell them someone wants to talk
            foreach (IdentityUser u in targetUsers)
            {
                u.InCall = true;
                await Clients.Client(u.ConnectionID).IncomingCall(callingUser);
            }
        }

        public async Task AnswerCall(bool acceptCall, IdentityUser targetConnectionId)
        {
            Room callingRoom = GetRoomByConnectionID(Context.ConnectionId);
            IdentityUser callingUser = callingRoom.UserCall.SingleOrDefault(u => u.ConnectionID == Context.ConnectionId);
            var targetUser = callingRoom.UserCall.SingleOrDefault(u => u.ConnectionID == targetConnectionId.ConnectionID);

            // This can only happen if the server-side came down and clients were cleared, while the user
            // still held their browser session.
            if (callingUser == null)
            {
                return;
            }

            // Make sure the original caller has not left the page yet
            if (targetUser == null)
            {
                await Clients.Caller.CallEnded(targetConnectionId, "The other user in your call has left.");
                return;
            }

            // Send a decline message if the callee said no
            if (acceptCall == false)
            {
                await Clients.Client(targetConnectionId.ConnectionID).CallDeclined(callingUser, string.Format("{0} did not accept your call.", callingUser.UserName));
                return;
            }

            // Make sure there is still an active offer.  If there isn't, then the other use hung up before the Callee answered.
            // Tell the original caller that the call was accepted
            await Clients.Client(targetConnectionId.ConnectionID).CallAccepted(callingUser);

            // Update the user list, since thes two are now in a call
            //await SendUserListUpdate();
        }

        public async Task HangUp()
        {
            Room callingRoom = GetRoomByConnectionID(Context.ConnectionId);
            IdentityUser callingUser = _Users.SingleOrDefault(u => u.ConnectionID == Context.ConnectionId);
            // if room is mine . Remove all user in call
            if (callingRoom.UserCall.Count == 1)
            {
                // do something
                VideoChatDBContext vcdb = new VideoChatDBContext();
                Room rm = GetRoomByConnectionID(callingUser.ConnectionID);
                vcdb.ClassRoom.Remove(vcdb.ClassRoom.SingleOrDefault(u=>u.ClassID == rm.RoomIF.ClassID));
            }
            // do something if not
            if (callingUser == null)
            {
                return;
            }

            // Send a hang up message to each user in the call, if there is one
            if (callingRoom != null)
            {
                foreach (IdentityUser user in callingRoom.UserCall.Where(u => u.ConnectionID != callingUser.ConnectionID))
                {
                    user.InCall = false;
                    user.IsCaller = false;
                    await Clients.Client(user.ConnectionID).CallEnded(callingUser, string.Format("{0} has hung up.", callingUser.UserName));
                }

            }
            await SendUserListUpdate(callingRoom);
        }

        // WebRTC Signal Handler
        public async Task SendSignal(string signal, string targetConnectionId)
        {
            Room callingRoom = GetRoomByConnectionID(Context.ConnectionId);
            IdentityUser callingUser = callingRoom.UserCall.SingleOrDefault(u => u.ConnectionID == Context.ConnectionId);
            IdentityUser targetUser = _Users.SingleOrDefault(u => u.ConnectionID == targetConnectionId);
            // Make sure both users are valid
            if (callingUser == null || targetUser == null)
            {
                return;
            }

            // These folks are in a call together, let's let em talk WebRTC
            await Clients.Client(targetConnectionId).ReceiveSignal(callingUser, signal);
        }

        #region Private Helpers

        private async Task SendUserListUpdate(Room rm)
        {
            await Clients.All.UpdateUserList(rm.UserCall);
        }
        private  Room GetRoomByConnectionID(string cid)
        {
            Room matchingRoom = _Rooms.SingleOrDefault(r => r.UserCall.SingleOrDefault(u => u.ConnectionID == cid) != null);
            return matchingRoom;
        }
        private Room GetRoomByClassID(ClassRoom rm)
        {
            Room matchingRoom = _Rooms.SingleOrDefault(r => r.RoomIF.ClassID == rm.ClassID);
            return matchingRoom;
        }

        #endregion
    }
    public interface IConnectionHub
    {
        Task UpdateUserList(List<IdentityUser> userList);
        Task CallAccepted(IdentityUser acceptingUser);
        Task CallDeclined(IdentityUser decliningUser, string reason);
        Task IncomingCall(IdentityUser callingUser);
        Task ReceiveSignal(IdentityUser signalingUser, string signal);
        Task CallEnded(IdentityUser signalingUser, string signal);
    }
}
