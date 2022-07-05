using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Avatar2;
using UnityEngine;
using Photon.Pun;
using CAPI = Oculus.Avatar2.CAPI;
using static Oculus.Avatar2.OvrAvatarHelperExtensions;
#if UNITY_EDITOR
using UnityEditor;
#endif



// Modified from https://forums.oculusvr.com/t5/Unity-VR-Development/Meta-avatar-2-multiplayer/m-p/937397

namespace MILab.MetaverseBase
{
    public class PhotonAvatarEntity : OvrAvatarEntity, IPunObservable
    {
        PhotonView m_photonView;
        List<byte[]> m_streamedDataList = new List<byte[]>();
        int m_maxBytesToLog = 15;
        [SerializeField] ulong m_instantiationData;
        float m_cycleStartTime = 0;
        float m_intervalToSendData = 0.04f;

        Vector3 receivedPosition;
        Quaternion receivedRotation;

        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;

        public enum AssetSource
        {
            Zip,
            StreamingAssets,
        }

        [System.Serializable]
        private struct AssetData
        {
            public AssetSource source;
            public string path;
        }
#pragma warning disable CS0414
        [Tooltip("Asset suffix for non-Android platforms")]
        [SerializeField] private string _assetPostfixDefault = "_rift.glb";
        [Tooltip("Asset suffix for Android platforms")]
        [SerializeField] private string _assetPostfixAndroid = "_quest.glb";
#pragma warning restore CS0414

        //Asset paths to load, and whether each asset comes from a preloaded zip file or directly from StreamingAssets
        private List<AssetData> _assets = new List<AssetData> { new AssetData { source = AssetSource.Zip, path = "" } };


        //Avatar Numbers for avatars we are using by default
        int[] avatarNumArray = { 7, 8, 4, 5, 12, 14, 31, 19, 18, 23 };

        public int currentAvatarNum;

        protected override void Awake()
        {
            ConfigureAvatarEntity();
            base.Awake();

            // Keep track of the localPlayer 
            if (m_photonView.IsMine)
            {
                LocalPlayerInstance = gameObject;
            }

            if (OVRManager.instance != null)
            {
                receivedPosition = OVRManager.instance.transform.position;
                receivedRotation = OVRManager.instance.transform.rotation;
            }
        }

        private void Start()
        {
            StartCoroutine(TryToLoadUser());

        }

        void ConfigureAvatarEntity()
        {
            m_photonView = GetComponent<PhotonView>();

            if (m_photonView.IsMine || !PhotonNetwork.IsConnected)
            {
                SetIsLocal(true);
                _creationInfo.features = Oculus.Avatar2.CAPI.ovrAvatar2EntityFeatures.Preset_Default;

                SampleInputManager sampleInputManager = OvrAvatarManager.Instance.gameObject.GetComponent<SampleInputManager>();
                SetBodyTracking(sampleInputManager);
                OvrAvatarLipSyncContext lipSyncInput = GameObject.FindObjectOfType<OvrAvatarLipSyncContext>();
                SetLipSync(lipSyncInput);
                gameObject.name = "LocalAvatar";
            }
            else
            {
                SetIsLocal(false);
                _creationInfo.features = Oculus.Avatar2.CAPI.ovrAvatar2EntityFeatures.Preset_Remote;
                gameObject.name = "RemoteAvatar";
            }
        }

        private void LoadLocalAvatar()
        {
            string assetPostfix = OvrAvatarManager.IsAndroidStandalone ? _assetPostfixAndroid : _assetPostfixDefault;
            //string assetPostfix = OvrAvatarManager.Instance.GetPlatformGLBPostfix() + ".glb";

            // Zip asset paths are relative to the inside of the zip.
            // Zips can be loaded from the OvrAvatarManager at startup or by calling OvrAvatarManager.Instance.AddZipSource
            // Assets can also be loaded individually from Streaming assets
            var path = new string[1];
            foreach (var asset in _assets)
            {
                int avatarNum;
                if (PhotonNetwork.IsConnected)
                    //avatarNum = m_photonView.Owner.ActorNumber % 32;
                    avatarNum = avatarNumArray[m_photonView.Owner.ActorNumber % avatarNumArray.Length];
                else
                    avatarNum = 32;

                currentAvatarNum = avatarNum;

                path[0] = avatarNum + assetPostfix;
                switch (asset.source)
                {
                    case AssetSource.Zip:
                        LoadAssetsFromZipSource(path);
                        break;
                    case AssetSource.StreamingAssets:
                        LoadAssetsFromStreamingAssets(path);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        IEnumerator TryToLoadUser()
        {
            LoadLocalAvatar();
            yield return null;
        }


        public void LoadLocalAvatarByNum(int num)
        {
            string assetPostfix = OvrAvatarManager.IsAndroidStandalone ? _assetPostfixAndroid : _assetPostfixDefault;
            //string assetPostfix = OvrAvatarManager.Instance.GetPlatformGLBPostfix() + ".glb";

            // Zip asset paths are relative to the inside of the zip.
            // Zips can be loaded from the OvrAvatarManager at startup or by calling OvrAvatarManager.Instance.AddZipSource
            // Assets can also be loaded individually from Streaming assets
            var path = new string[1];
            foreach (var asset in _assets)
            {
                if (num > 32 || num < 0)
                {
                    num = 0;
                }
                currentAvatarNum = num;

                path[0] = num + assetPostfix;
                switch (asset.source)
                {
                    case AssetSource.Zip:
                        LoadAssetsFromZipSource(path);
                        break;
                    case AssetSource.StreamingAssets:
                        LoadAssetsFromStreamingAssets(path);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        void RecordAndSendStreamDataIfMine()
        {
            if (m_photonView.IsMine)
            {
                byte[] bytes = RecordStreamData(activeStreamLod);
                m_photonView.RPC("RecieveStreamData", RpcTarget.Others, bytes);
            }
        }

        [PunRPC]
        public void RecieveStreamData(byte[] bytes)
        {
            m_streamedDataList.Add(bytes);
        }

        void LogFirstFewBytesOf(byte[] bytes)
        {
            for (int i = 0; i < m_maxBytesToLog; i++)
            {
                string bytesString = Convert.ToString(bytes[i], 2).PadLeft(8, '0');
            }
        }


        private void Update()
        {
            // Set transform to follow the Camera Rig if this is the local player otherwise use the received data
            if (m_photonView.IsMine)
            {
                if (OVRManager.instance != null)
                {
                    this.transform.position = OVRManager.instance.transform.position;
                    this.transform.rotation = OVRManager.instance.transform.rotation;
                }
            }
            else
            {
                this.transform.position = receivedPosition;
                this.transform.rotation = receivedRotation;
            }

            if (m_streamedDataList.Count > 0)
            {
                if (IsLocal == false)
                {
                    byte[] firstBytesInList = m_streamedDataList[0];
                    if (firstBytesInList != null)
                    {
                        ApplyStreamData(firstBytesInList);
                    }
                    m_streamedDataList.RemoveAt(0);
                }
            }
        }

        private void LateUpdate()
        {
            float elapsedTime = Time.time - m_cycleStartTime;
            if (elapsedTime > m_intervalToSendData)
            {
                RecordAndSendStreamDataIfMine();
                m_cycleStartTime = Time.time;
            }

        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting && m_photonView.IsMine)
            {
                if (OVRManager.instance != null)
                {
                    // Send position and rotation
                    stream.SendNext(OVRManager.instance.transform.position);
                    stream.SendNext(OVRManager.instance.transform.rotation);
                }
            }
            else
            {
                // Receive the position and rotation 
                receivedPosition = (Vector3)stream.ReceiveNext();
                receivedRotation = (Quaternion)stream.ReceiveNext();
            }
        }

        public void SetVisibility(bool visible)
        {
            if (visible)
            {
                SetActiveView(CAPI.ovrAvatar2EntityViewFlags.ThirdPerson);
            }
            else
            {
                SetActiveView(CAPI.ovrAvatar2EntityViewFlags.None);
            }
        }

        // Replace avatar
        public void ChangeAvatar(int num)
        {
            m_photonView.RPC("RPC_ChangeAvatar", RpcTarget.AllBufferedViaServer, num);
        }

        [PunRPC]
        public void RPC_ChangeAvatar(int num)
        {
            Teardown();
            CreateEntity();
            LoadLocalAvatarByNum(num);
        }
    }
}