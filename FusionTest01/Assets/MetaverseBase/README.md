# Metaverse Base
This package will help you start developing for the MI Lab's Metaverse.

## Dependencies
You will need to add these packages and configurations to get started.

### Oculus Integration
Import the [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022) package from the Unity Asset Store. See the [getting started guide](https://developer.prod.oculus.com/documentation/unity/unity-gs-overview/) from Oculus for more information.

### Meta Avatars
Download the [Meta Avatars SDK](https://developer.prod.oculus.com/downloads/package/meta-avatars-sdk) from the Oculus website and import it into Unity. See the Oculus [guide on avatars](https://developer.oculus.com/documentation/unity/meta-avatars-overview/) for more information.

### Photon
Import the [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) package from the Unity Asset Store. This package includes assets you will need to use Photon's PUN 2 and voice chat systems. You will also need to create an app key for PUN and an app key for voice chat. See the [PUN documentation](https://doc.photonengine.com/en-us/pun/current/getting-started/pun-intro) and [voice chat documentation](https://doc.photonengine.com/en-us/voice/v2/getting-started/voice-intro) for more information.

## How to Use
The Core scene provides the basic functionality for loading in avatars and starting the Photon session. All content you add should be placed in a separate scene. Place the CalibrationArea prefab in the content scene at a consistent reference place in the real world such as a wall.

To load your content scene, edit the SceneController's first scene field to be the name of your content scene. If you want to add different core functionality, I reccomend creating a copy of the Core scene and putting the modified copy first in the scene build index. 

## Notes
- The Core functionality scene should always come first in the scene build index so that it loads first.
- Make sure the content scene is also in the scene build index.
- Make sure the Light in the Avatar Lighting of the Core scene has its Culling Mask set to only the Avatar layer.
- Make sure the MetaPhotonAvatar prefab in the Resources folder has its layer set to Avatar too.