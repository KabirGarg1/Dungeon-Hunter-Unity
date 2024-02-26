using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;


public class RoomNodeGraphEditor : EditorWindow
{
    private GUIStyle roomNodeStyle;
    private GUIStyle roomNodeSelectedStyle;
    private static RoomNodeGraphSO currentRoomNodeGraph;

    private Vector2 graphDrag;
    private Vector2 graphOffSet;
    private RoomNodeSO currentRoomNode = null;
    private  RoomNodeTypeListSO roomNodeTypeList;

    
    private const float nodeWidth = 160f;
    private const float nodeHeight = 75f;
    private const int  nodePadding = 25;
    private const int nodeBorder = 12;

    private const float connectingLineWidth = 3f;
    private const float arrowSize = 6f;

    private const float gridLarge = 100f;
    private const float gridSmall = 25f;


   [MenuItem("Room Node Graph Editor",menuItem = "Window/Dungeon Editor/Room Node Editor Graph")]
    private static void OpenWindow()
    {
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }

    private void OnEnable()
    {
        Selection.selectionChanged += InspectorSelectionChanged;


        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        roomNodeStyle.normal.textColor = Color.white;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding); 
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        roomNodeSelectedStyle = new GUIStyle();
        roomNodeSelectedStyle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
        roomNodeSelectedStyle.normal.textColor = Color.white;
        roomNodeSelectedStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeSelectedStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }
    private void OnDisable()
    {
        Selection.selectionChanged -= InspectorSelectionChanged;
    }

    [OnOpenAsset(0)]

    public static bool OnDoubleClick(int instanceID, int line)
    {
        RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;

        if (roomNodeGraph != null)
        {
            OpenWindow();
            currentRoomNodeGraph = roomNodeGraph;
            return true;
        }
        return false;
    }
    private void OnGUI()
    {
        if (currentRoomNodeGraph != null)
        {
            DrawBackGroundGrid(gridSmall, 0.2f, Color.gray);
            DrawBackGroundGrid(gridLarge, 0.35f, Color.gray);
            DrawDraggedLine();
            ProcessEvents(Event.current);
            DrawRoomConnections();
            DrawRoomNodes();
        }
        if (GUI.changed) 
            Repaint();
    }

    private void DrawBackGroundGrid(float gridSize, float gridOpacity, Color gridColor)
    {
        int verticalLineCount = Mathf.CeilToInt((position.width + gridSize) / gridSize);
        int horizontalLineCount = Mathf.CeilToInt((position.height + gridSize) / gridSize);

        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
        graphOffSet += graphDrag * 0.5f;
        Vector3 gridOffSet = new Vector3(graphOffSet.x%gridSize,graphOffSet.y%gridSize,0);
        for (int i = 0; i < verticalLineCount; i++)
        {
            Handles.DrawLine(new Vector3(gridSize * i, -gridSize, 0f)+gridOffSet, new Vector3(gridSize*i,position.height+gridSize,0f)+gridOffSet) ;
        }
        for (int j = 0; j < horizontalLineCount; j++)
        {
            Handles.DrawLine(new Vector3(-gridSize,gridSize*j, 0f) + gridOffSet, new Vector3(position.width+gridSize, gridSize * j, 0f)+gridOffSet);
        }
        Handles.color = Color.white;
    }

    private void DrawDraggedLine()
    {
        if(currentRoomNodeGraph.linePosition != Vector2.zero)
        {
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition,
                              currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, Color.white, null, connectingLineWidth);
        }
    }

    private void ProcessEvents(Event currentEvent)
    {
        graphDrag = Vector2.zero;
        if (currentRoomNode == null || currentRoomNode.isLeftClickDragging == false)
        {
            currentRoomNode = IsMouseOverRoomNode(currentEvent);
        }
        if (currentRoomNode == null  || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            ProcessRoomNodeGraphEvents(currentEvent);
        }
        else
        {
            currentRoomNode.ProcessEvents(currentEvent);
        }
    }


    private RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
    {
        for (int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--)
        {
            if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
            {
                return currentRoomNodeGraph.roomNodeList[i];
            }
        }
        return null;
    }
    private void ProcessRoomNodeGraphEvents(Event currentEvent)
    {
        switch(currentEvent.type)
        { 
           case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent); break;
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent); break;
           case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent); break;

           default : break;
        }
    }

    private void ProcessMouseUpEvent(Event currentEvent)
    {
        if(currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);
            if(roomNode != null)
            {
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeIDToRoomNode(roomNode.id))
                {
                    roomNode.AddParentRoomNodeIDToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }
            ClearLineDrag();
        }
    }

    private void ClearLineDrag()
    {
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        currentRoomNodeGraph.linePosition = Vector2.zero;
        GUI.changed = true;
    }
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        if(currentEvent.button == 1)
        {
            ProcessRightMouseDragEvent(currentEvent);
        }
        if(currentEvent.button == 0)
        {
            ProcessLeftMouseDragEvent(currentEvent.delta);
        }
    }

    private void ProcessLeftMouseDragEvent(Vector2 dragDelta)
    {
        graphDrag = dragDelta;

        for(int i = 0; i < currentRoomNodeGraph.roomNodeList.Count; i++)
        {
            currentRoomNodeGraph.roomNodeList[i].DragNode(dragDelta);
        }
        GUI.changed = true;
    }
    private void ProcessRightMouseDragEvent(Event currentEvent)
    {
        if(currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(currentEvent.delta);
            GUI.changed = true;
        }
    }
    
    private void  DragConnectingLine(Vector2 mousePosition)
    {
        currentRoomNodeGraph.linePosition += mousePosition;
    }
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        if (currentEvent.button == 1)
        {
            ShowContextMenu(currentEvent.mousePosition);
        }
        else if (currentEvent.button == 0)
        {
            ClearLineDrag();
            ClearAllSelectedRoomNodes();
        }
    }
    private void ClearAllSelectedRoomNodes()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.isSelected = false;
                GUI.changed = true;
            }
        }
    }
    private void SelectAllRoomNodes()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.isSelected=true;
        }
        GUI.changed = true;
    }

    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new();
        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Delete Selected Room Node Links"), false, DeleteSelectedRoomNodeLinks);
        menu.AddItem(new GUIContent("Delete Selected Room Nodes"), false, DeleteSelectedRoomNodes);



        menu.ShowAsContext();           
    }

    private void DeleteSelectedRoomNodes()
    {
        Queue<RoomNodeSO> roomNodeDeletionQueue = new Queue<RoomNodeSO>();
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected && !roomNode.roomNodeType.isEntrance)
            {
                roomNodeDeletionQueue.Enqueue(roomNode);

                foreach(string childRoomNodeID in roomNode.childrenRoomNodeIDlist)
                {
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(childRoomNodeID);
                    if(childRoomNode != null)
                    {
                        childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
                foreach(string parentRoomNodeID in roomNode.parentRoomNodeIDlist)
                {
                    RoomNodeSO parentRoomNode = currentRoomNodeGraph.GetRoomNode(parentRoomNodeID);
                    if(parentRoomNode != null)
                    {
                        parentRoomNode.RemoveChildRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
            }
        }
        while(roomNodeDeletionQueue.Count > 0)
        {
            RoomNodeSO roomNodeToDelete = roomNodeDeletionQueue.Dequeue();
            currentRoomNodeGraph.roomNodeDictionary.Remove(roomNodeToDelete.id);
            currentRoomNodeGraph.roomNodeList.Remove(roomNodeToDelete);
            DestroyImmediate(roomNodeToDelete, true);
            AssetDatabase.SaveAssets();
        }
    }

    private void DeleteSelectedRoomNodeLinks()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if(roomNode.isSelected && roomNode.childrenRoomNodeIDlist.Count > 0)
            {
                for(int i = roomNode.childrenRoomNodeIDlist.Count - 1; i >= 0; i--)
                {
                    RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(roomNode.childrenRoomNodeIDlist[i]);
                    if(childRoomNode != null && childRoomNode.isSelected)
                    {
                        roomNode.RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);
                        childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
                    }
                }
            }
        }
    }

    private void CreateRoomNode(object mousePositionObject)
    {
        if(currentRoomNodeGraph.roomNodeList.Count == 0)
        {
            CreateRoomNode(new Vector2(200f,200f),roomNodeTypeList.list.Find(x => x.isEntrance));
        }
        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }
    private void CreateRoomNode(object mousePositionObject,RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2)mousePositionObject;

        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        roomNode.initialise(new Rect(mousePosition,new Vector2(nodeWidth,nodeHeight)),currentRoomNodeGraph,roomNodeType);

        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);
        AssetDatabase.SaveAssets();

        currentRoomNodeGraph.OnValidate();
    }
    private void DrawRoomNodes()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.isSelected)
            {
                roomNode.Draw(roomNodeSelectedStyle);
            }
            else
            {
                roomNode.Draw(roomNodeStyle);
            }
        }
        GUI.changed = true;
    }
    private void DrawRoomConnections()
    {
        foreach(RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList )
        {
            if(roomNode.childrenRoomNodeIDlist.Count > 0)
            { 
                foreach(string childRoomNodeID in roomNode.childrenRoomNodeIDlist)
                {
                    if (currentRoomNodeGraph.roomNodeDictionary.ContainsKey(childRoomNodeID))
                    {
                        DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childRoomNodeID]);
                        GUI.changed = true;
                    }
                }
            }
                
        }
    }
    private void DrawConnectionLine(RoomNodeSO parentRoomNode,RoomNodeSO childRoomNOde)
    {
        Vector2 startPosition = parentRoomNode.rect.center;
        Vector2 endPosition = childRoomNOde.rect.center;

        Vector2 midPosition = (endPosition+startPosition)/2f;
        Vector2 direction = endPosition - startPosition;

        Vector2 arrowTailPoint1 = midPosition - new Vector2(-direction.y, direction.x).normalized * arrowSize;
        Vector2 arrowTailPoint2 = midPosition + new Vector2(-direction.y, direction.x).normalized * arrowSize;
        Vector2 arrowHead = midPosition + direction.normalized * arrowSize;

        Handles.DrawBezier(arrowHead, arrowTailPoint1, arrowHead,arrowTailPoint1,Color.white,null,connectingLineWidth);
        Handles.DrawBezier(arrowHead, arrowTailPoint2, arrowHead, arrowTailPoint2, Color.white, null, connectingLineWidth);

        Handles.DrawBezier(startPosition, endPosition,startPosition,endPosition,Color.white,null,connectingLineWidth);
        GUI.changed = true;
    }

    private void InspectorSelectionChanged()
    {
        RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;
        if (roomNodeGraph != null)
        {
            currentRoomNodeGraph = roomNodeGraph;
            GUI.changed = true;
        }
    }
}