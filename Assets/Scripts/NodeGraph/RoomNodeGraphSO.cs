using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CreateAssetMenu(fileName =  "RoomNodeGraph",menuName = "Scriptable Objects/Dungeon/Room Node Graph")]
public class RoomNodeGraphSO : ScriptableObject
{
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;
    [HideInInspector] public List<RoomNodeSO> roomNodeList;
    [HideInInspector] public Dictionary<string, RoomNodeSO> roomNodeDictionary = new Dictionary<string, RoomNodeSO>();

    private void Awake()
    {
        LoadRoomNodeDictionary();
    }
    private void LoadRoomNodeDictionary()
    {
        roomNodeDictionary.Clear();
        foreach(RoomNodeSO roomNode in roomNodeList)
        {
            roomNodeDictionary[roomNode.id] = roomNode;
        }
    }

    public RoomNodeSO GetRoomNode(RoomNodeTypeSO roomNodeType)
    {
        foreach (RoomNodeSO roomNode in roomNodeList)
        {
            if(roomNode.roomNodeType == roomNodeType)
            {
                return roomNode;
            }
        }
        return null;
    }

    public RoomNodeSO GetRoomNode(string id)
    {
        if (roomNodeDictionary.TryGetValue(id, out RoomNodeSO roomNode)) { return roomNode; }
        return null;
    }

    public IEnumerable<RoomNodeSO> GetChildRoomNodes(RoomNodeSO parentRoomNode) 
    { 
        foreach(string childNodeID in parentRoomNode.childrenRoomNodeIDlist)
        {
            yield return GetRoomNode(childNodeID);
        }
    }

#if UNITY_EDITOR
    [HideInInspector] public RoomNodeSO roomNodeToDrawLineFrom = null;
    [HideInInspector] public Vector2 linePosition;

    public void OnValidate()
    {
        LoadRoomNodeDictionary();
    }
    public void SetNodeToDrawConnectionLineFrom(RoomNodeSO roomNode,Vector2 position)
    {
        roomNodeToDrawLineFrom = roomNode;
        linePosition = position;
    }

#endif

}
