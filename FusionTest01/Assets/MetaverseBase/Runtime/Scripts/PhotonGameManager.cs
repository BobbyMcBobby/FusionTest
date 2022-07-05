using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;

// This game object manages the Photon connection
// This script requires that an OVRCameraRig be in the scene and have the OVRManager script attached

namespace MILab.MetaverseBase
{
    public class PhotonGameManager : MonoBehaviourPunCallbacks
    {
        [Tooltip("The maximum number of players per room")]
        [SerializeField]
        private byte maxPlayersPerRoom = 8;

        [Tooltip("The name of the Photon room to create or join")]
        [SerializeField]
        string photonRoomName = "TestRoom";

        [Tooltip("Prefab that represents the player avatars")]
        [SerializeField]
        GameObject networkedPlayerPrefab;

        [HideInInspector]
        static public GameObject localPlayer;

        // Singleton reference
        static public PhotonGameManager Instance { get; private set; }


        // Flag to track connection progress
        bool isConnecting;

        // This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
        string gameVersion = "1";

        void Awake()
        {
            // If there is an instance, and it's not me, delete myself.
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        void Start()
        {
            Connect();
        }

        // Called from within the OnJoinedRoom callback
        void SetUpPlayer()
        {
            if (networkedPlayerPrefab == null)
            {
                Debug.LogError("Missing playerPrefab in PhotonGameManager");
            }
            else
            {
                Debug.Log("Connected and instantiating player");
                localPlayer = PhotonNetwork.Instantiate(networkedPlayerPrefab.name, Vector3.zero, Quaternion.identity, 0);
            }
        }

        public void Connect()
        {
            isConnecting = true;

            // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.JoinOrCreateRoom(photonRoomName, new RoomOptions { MaxPlayers = this.maxPlayersPerRoom }, null);
            }
            else
            {
                // #Critical, we must first and foremost connect to Photon Online Server.
                PhotonNetwork.ConnectUsingSettings();
                PhotonNetwork.GameVersion = this.gameVersion;
            }
        }

        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
        }

        #region MonoBehaviourPunCallbacks CallBacks
        // Called after the connection to the master is established and authenticated
        public override void OnConnectedToMaster()
        {
            // we don't want to do anything if we are not attempting to join a room. 
            // this case where isConnecting is false is typically when you lost or quit the game, when this level is loaded, OnConnectedToMaster will be called, in that case
            // we don't want to do anything.
            if (isConnecting)
            {
                Debug.Log("OnConnectedToMaster: Next -> try to Join Room");

                // #Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRoomFailed()
                PhotonNetwork.JoinOrCreateRoom(photonRoomName, new RoomOptions { MaxPlayers = this.maxPlayersPerRoom }, null);
            }
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.Log("Failed to Join Room");
        }


        // Called after disconnecting from the Photon server.
        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogError("Photon Game Manager Disconnected: " + cause);
            isConnecting = false;
        }

        // Called when entering a room (by creating or joining it). Called on all clients (including the Master Client).
        public override void OnJoinedRoom()
        {
            Debug.Log("Green>OnJoinedRoom with " + PhotonNetwork.CurrentRoom.PlayerCount + " Player(s)");
            Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room.\nFrom here on, your game would be running.");
            SetUpPlayer();

            // Load first scene
            SceneController.Instance.GetComponent<PhotonView>().RPC("RPC_LoadScene", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, SceneController.Instance.firstSceneName);
        }

        public override void OnLeftRoom()
        {
            // TODO: Handle leaving the room
        }
        #endregion
    }

}