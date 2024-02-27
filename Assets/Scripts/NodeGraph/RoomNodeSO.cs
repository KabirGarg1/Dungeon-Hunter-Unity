using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering.UI;

public class RoomNodeSO : ScriptableObject
{
    [HideInInspector] public string id;
    [HideInInspector] public List<string> parentRoomNodeIDlist = new List<string>();
    [HideInInspector] public List<string> childrenRoomNodeIDlist = new List<string>();
    [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
    public RoomNodeTypeSO roomNodeType;
    [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;



#if UNITY_EDITOR
    [HideInInspector] public Rect rect;
    [HideInInspector] public bool isLeftClickDragging = false;
    [HideInInspector] public bool isSelected = false;

    public void initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
    {
        this.rect = rect;
        this.id = Guid.NewGuid().ToString();
        this.name = "RoomNode";
        this.roomNodeType = roomNodeType;
        this.roomNodeGraph = nodeGraph;

        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    public void Draw(GUIStyle nodeStyle)
    {
        GUILayout.BeginArea(rect, nodeStyle);
        EditorGUI.BeginChangeCheck();

        if (parentRoomNodeIDlist.Count > 0 || roomNodeType.isEntrance)
        {
            EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
        }
        else
        {
            int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
            int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

            roomNodeType = roomNodeTypeList.list[selection];

            if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isCorridor
                && roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
            {
                if (childrenRoomNodeIDlist.Count > 0)
                {
                    for (int i = childrenRoomNodeIDlist.Count - 1; i >= 0; i--)
                    {
                        RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childrenRoomNodeIDlist[i]);
                        if (childRoomNode != null)
                        {
                            RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);
                            childRoomNode.RemoveParentRoomNodeIDFromRoomNode(id);
                        }
                    }
                }
            }
        }
        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(this);

        GUILayout.EndArea();

    }

    public string[] GetRoomNodeTypesToDisplay()
    {
        string[] roomArray = new string[roomNodeTypeList.list.Count];
        for (int i = 0; i < roomNodeTypeList.list.Count; i++)
        {
            if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
            {
                roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
            }
        }
        return roomArray;
    }

    public void ProcessEvents(Event currentEvent)
    {
        switch(currentEvent.type)
        {
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);break;
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent); break;
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent); break;
            default: break;
        }
    }
    
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickDownEvent();
        }
        else if (currentEvent.button == 1)
        {
            ProcessRightClickDownEvent(currentEvent);
        }
    }

    private void ProcessRightClickDownEvent(Event currentEvent)
    {
        roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
    }

    private void ProcessLeftClickDownEvent()
    {
        Selection.activeObject = this;

        //isSelected = !isSelected;
        if (isSelected == true)
        {
            isSelected = false;
        }
        else
        {
            isSelected = true;
        }
    }

    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if (currentEvent.button == 0)
        {
            ProcessLeftClickUpEvent();
        }
    }

    private void ProcessLeftClickUpEvent()
    {
        if (isLeftClickDragging) { isLeftClickDragging = false; }
    }

    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if(currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent);
        }
    }

    private void ProcessLeftMouseDragEvent(Event currentEvent)
    {
        isLeftClickDragging = true;
        DragNode(currentEvent.delta);
        GUI.changed = true;
    }

    public void DragNode(Vector2 delta)
    {
        rect.position += delta;
        EditorUtility.SetDirty(this);
    }

    public bool AddChildRoomNodeIDToRoomNode(string ChildID)
    {
        if (IsChildRoomValid(ChildID))
        {
            childrenRoomNodeIDlist.Add(ChildID);
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool IsChildRoomValid(string ChildID)
    {
        bool isConnectedBossNodeAlready = false;
        foreach(RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
        {
            if(roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDlist.Count > 0)
            {
                isConnectedBossNodeAlready = true;
            }
        }
        if (roomNodeGraph.GetRoomNode(ChildID) && isConnectedBossNodeAlready)
            return false;
        if (roomNodeGraph.GetRoomNode(ChildID).roomNodeType.isNone) 
            return false;
        if (childrenRoomNodeIDlist.Contains(ChildID)) 
            return false;
        if(id == ChildID)
            return false;
        if(parentRoomNodeIDlist.Contains(ChildID))
            return false;
        if(roomNodeGraph.GetRoomNode(ChildID).parentRoomNodeIDlist.Count > 0) 
            return false; 
        if(roomNodeGraph.GetRoomNode(ChildID).roomNodeType.isCorridor && roomNodeType.isCorridor)
            return false;
        if (!roomNodeGraph.GetRoomNode(ChildID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
            return false;
        if (roomNodeGraph.GetRoomNode(ChildID).roomNodeType.isCorridor && childrenRoomNodeIDlist.Count> Settings.maxChildCorridors)
            return false;
        if (roomNodeGraph.GetRoomNode(ChildID).roomNodeType.isEntrance)
            return false;
        if (!roomNodeGraph.GetRoomNode(ChildID).roomNodeType.isCorridor && childrenRoomNodeIDlist.Count > 0)
            return false;
        return true;
    }
    
    public bool AddParentRoomNodeIDToRoomNode(string ParentID)
    {
        parentRoomNodeIDlist.Add(ParentID); return true;
    }

    public bool RemoveChildRoomNodeIDFromRoomNode(string ChildID)
    {
        if (childrenRoomNodeIDlist.Contains(ChildID))
        {
            childrenRoomNodeIDlist.Remove(ChildID);
            return true;
        }
        return false;
    }
    public bool RemoveParentRoomNodeIDFromRoomNode(string ParentID)
    {
        if (parentRoomNodeIDlist.Contains(ParentID))
        {
            parentRoomNodeIDlist.Remove(ParentID);
            return true;
        }
        return false;

    }
#endif
}