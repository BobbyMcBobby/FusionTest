using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MILab.MetaverseBase
{
    public class SceneController : MonoBehaviour
    {
        PhotonView photonView;

        // Each actor number is associated with a list of names of scenes that client has open
        Dictionary<int, List<string>> clientScenes;

        // Each photon viewID of an avatar is associated with its owning actor number
        Dictionary<int, int> clientAvatars;

        static public SceneController Instance { get; private set; }

        [Tooltip("The name of the content scene to load first")]
        public string firstSceneName;

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
                DontDestroyOnLoad(this);
                clientScenes = new Dictionary<int, List<string>>();
                clientAvatars = new Dictionary<int, int>();
                photonView = this.GetComponent<PhotonView>();
            }
        }

        void Start()
        {
        }

        void Update()
        {

        }

        #region Loading 
        // This RPC is called by a client adding a virtual scene
        [PunRPC]
        public void RPC_LoadScene(int actorNum, string sceneName)
        {
            // Add to list of scenes the client is in
            if (!clientScenes.ContainsKey(actorNum))
            {
                clientScenes.Add(actorNum, new List<string>());
            }
            if (!clientScenes[actorNum].Contains(sceneName))
            {
                clientScenes[actorNum].Add(sceneName);
            }

            // If the loading client is this one, change scenes and hide other clients that don't share a scene
            if (actorNum == PhotonNetwork.LocalPlayer.ActorNumber)
            {

                // Start loading the next scene
                StartCoroutine(AsyncAdditiveSceneLoadCoroutine(sceneName));

                // Check each client and show them if a scene is now shared
                foreach (int client in clientScenes.Keys)
                {
                    // Skip the client doing the loading 
                    if (client == actorNum) continue;

                    bool sharedScene = false;
                    foreach (string s1 in clientScenes[client])
                    {
                        foreach (string s2 in clientScenes[actorNum])
                        {
                            if (s1.Equals(s2))
                            {
                                sharedScene = true;
                            }
                        }
                    }

                    int avatarID = clientAvatars[client];
                    var avatar = PhotonView.Find(avatarID).gameObject.GetComponent<PhotonAvatarEntity>();
                    if (sharedScene)
                    {
                        //avatar.SetActive(true);
                        avatar.SetVisibility(true);
                    }
                    else
                    {
                        //avatar.SetActive(false);
                        avatar.SetVisibility(false);
                    }
                }
            }
            else
            {
                // Check if other client loaded into a scene this one is active in
                if (clientScenes[PhotonNetwork.LocalPlayer.ActorNumber].Contains(sceneName))
                {
                    int avatarID = clientAvatars[actorNum];
                    var avatar = PhotonView.Find(avatarID).gameObject.GetComponent<PhotonAvatarEntity>();
                    avatar.SetVisibility(true);
                    //avatar.SetActive(true);
                }
            }

            // TODO: track anything that other clients need to know about someone else loading a scene

        }

        IEnumerator AsyncAdditiveSceneLoadCoroutine(string scene)
        {
            Debug.Log("Begin loading scene: " + scene);
            var async = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            async.allowSceneActivation = false;

            while (async.progress < 0.9f)
            {
                Debug.Log(scene + " load progress: " + async.progress);
                yield return null;
            }
            async.allowSceneActivation = true;

            // TODO: issue some event to indicate load is complete
        }
        #endregion

        #region Unloading
        // RPC called by a client leaving a virtual scene
        [PunRPC]
        public void RPC_UnloadScene(int actorNum, string sceneName)
        {

            if (!clientScenes.ContainsKey(actorNum))
            {
                clientScenes.Add(actorNum, new List<string>());
            }
            if (clientScenes[actorNum].Contains(sceneName))
            {
                clientScenes[actorNum].Remove(sceneName);
            }


            // If the unloading client is this one, start the scene process
            if (actorNum == PhotonNetwork.LocalPlayer.ActorNumber)
            {

                StartCoroutine(AsyncSceneUnloadCoroutine(sceneName));


                // Check each client and hide them if a scene is no longer shared
                foreach (int client in clientScenes.Keys)
                {
                    // Skip the client doing the unloading 
                    if (client == actorNum) continue;

                    bool sharedScene = false;
                    foreach (string s1 in clientScenes[client])
                    {
                        foreach (string s2 in clientScenes[actorNum])
                        {
                            if (s1.Equals(s2))
                            {
                                sharedScene = true;
                            }
                        }
                    }

                    int avatarID = clientAvatars[client];
                    var avatar = PhotonView.Find(avatarID).gameObject.GetComponent<PhotonAvatarEntity>();
                    if (sharedScene)
                    {
                        //avatar.SetActive(true);
                        avatar.SetVisibility(true);
                    }
                    else
                    {
                        //avatar.SetActive(false);
                        avatar.SetVisibility(false);
                    }
                }
            }
            else
            {
                // Check if actorNum client has any scenes in comon now
                if (clientScenes[PhotonNetwork.LocalPlayer.ActorNumber].Contains(sceneName))
                {
                    bool sharedScene = false;
                    foreach (string s1 in clientScenes[PhotonNetwork.LocalPlayer.ActorNumber])
                    {
                        foreach (string s2 in clientScenes[actorNum])
                        {
                            if (s1.Equals(s2))
                            {
                                sharedScene = true;
                            }
                        }
                    }

                    int avatarID = clientAvatars[actorNum];
                    var avatar = PhotonView.Find(avatarID).gameObject.GetComponent<PhotonAvatarEntity>();
                    if (sharedScene)
                    {
                        //avatar.SetActive(true);
                        avatar.SetVisibility(true);
                    }
                    else
                    {
                        //avatar.SetActive(false);
                        avatar.SetVisibility(false);
                    }
                }
            }

        }

        IEnumerator AsyncSceneUnloadCoroutine(string scene)
        {
            Debug.Log("Begin unloading scene: " + scene);
            var async = SceneManager.UnloadSceneAsync(scene);

            while (async.progress < 0.9f)
            {
                Debug.Log(scene + " unload progress: " + async.progress);
                yield return null;
            }
            async.allowSceneActivation = false;

            // TODO: Issue some sort of event
        }
        #endregion

        // FIXME: Remove the avatar id from list if they disconnect
        [PunRPC]
        public void RPC_AddAvatar(int actorNum, int id)
        {
            Debug.Log("Adding avatar " + id + " owned by player " + actorNum + " actorNum");
            clientAvatars.Add(actorNum, id);
        }

    }
}
