using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;


[CreateAssetMenu(fileName ="DungeonLevel_",menuName ="Scriptable Objects/Dungeon/Dungeon Level")]
public class DungeonLevelSO : ScriptableObject
{
    public string levelName;
    public List<RoomTemplateSO> roomTemplateList;
    public List<RoomNodeGraphSO> roomNodeGraphList;
#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckEmptyString(this, nameof(levelName), levelName);
        if (HelperUtilities.ValidateCheckEnumerableValues(this, nameof(roomTemplateList), roomTemplateList)) { return; }
        if (HelperUtilities.ValidateCheckEnumerableValues(this, nameof(roomNodeGraphList), roomNodeGraphList)) { return; }

        bool isEWCorridor = false;
        bool isNSCorridor = false;
        bool isEntrance = false;

        foreach (RoomTemplateSO roomTemplate in roomTemplateList)
        {
            if (roomTemplate == null) 
            {
                Debug.Log("Template is Null in " + this.name.ToString());
                return;
            }
            if(roomTemplate.roomNodeType.isCorridorEW) { isEWCorridor = true;}
            if(roomTemplate.roomNodeType.isCorridorNS) {  isNSCorridor = true;}
            if(roomTemplate.roomNodeType.isEntrance) { isEntrance = true;}
        }
        if (isEWCorridor==false) 
        {
            Debug.Log("In" + this.name.ToString() + "No E/W Room type is specified");
        }
        if (isNSCorridor == false)
        {
            Debug.Log("In" + this.name.ToString() + "No N/S Room type is specified");
        }
        if (isEntrance == false)
        {
            Debug.Log("In" + this.name.ToString() + "No Entrance Room type is specified");
        }

        foreach(RoomNodeGraphSO roomNodeGraph in roomNodeGraphList)
        {
            if(roomNodeGraph == null) return;
            foreach(RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
            {
                if(roomNode == null) continue;
                if (roomNode.roomNodeType.isEntrance || roomNode.roomNodeType.isCorridor || roomNode.roomNodeType.isCorridorEW ||
                    roomNode.roomNodeType.isCorridorNS || roomNode.roomNodeType.isNone) continue;

                bool isRoomNodeTypeFound = false;

                foreach (RoomTemplateSO roomTemplate in roomTemplateList)
                {
                    if (roomTemplate == null) continue;
                    if (roomTemplate.roomNodeType == roomNode.roomNodeType)
                    {
                        isRoomNodeTypeFound = true;
                        break;
                    }
                }
                if (isRoomNodeTypeFound == false)
                {
                    Debug.Log("In "+this.name.ToString() + " : No room template "+roomNode.roomNodeType.name.ToString() + " found for node graph "+
                        roomNodeGraph.name.ToString() );
                }
            }

            
        }
    }

    
#endif


}
