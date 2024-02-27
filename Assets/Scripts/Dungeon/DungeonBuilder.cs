using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;


[DisallowMultipleComponent]
public class DungeonBuilder : SingletonMonobehaviour<DungeonBuilder>
{
    public Dictionary<string,Room> dungeonBuilderRoomDictionary = new Dictionary<string,Room>();
    public Dictionary<string,RoomTemplateSO> roomTemplateDictionary = new Dictionary<string,RoomTemplateSO>();
    private List<RoomTemplateSO> roomTemplateList = null;
    private RoomNodeTypeListSO roomNodeTypeList;
    private bool dungeonBuildSuccesful;

    private void OnEnable()
    {
        // Set dimmed material to off
        GameResources.Instance.dimmedMaterial.SetFloat("Alpha_Slider", 0f);
    }

    private void OnDisable()
    {
        // Set dimmed material to fully visible
        GameResources.Instance.dimmedMaterial.SetFloat("Alpha_Slider", 1f);
    }
    protected override void Awake()
    {
        base.Awake();
        LoadRoomNodeTypeList();
    }
    private void LoadRoomNodeTypeList()
    {
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }
    public bool GenerateDungeon(DungeonLevelSO currentDungeonLevel)
    {
        roomTemplateList = currentDungeonLevel.roomTemplateList;

        LoadRoomTemplateIntoDictionary();
        dungeonBuildSuccesful = false;
        int dungeonBuildAttempts = 0;
        while(!dungeonBuildSuccesful && dungeonBuildAttempts< Settings.maxDungeonBuildAttempts)
        {
            dungeonBuildAttempts++;
            RoomNodeGraphSO roomNodeGraph = SelectRandomRoomNodeGraph(currentDungeonLevel.roomNodeGraphList);

            int dungeonRebuildBuildAttempts = 0;
            dungeonBuildSuccesful = false;
            while (!dungeonBuildSuccesful && dungeonRebuildBuildAttempts <= Settings.maxDungeonRebuildAttemptsForRoomGraph)
            {
                ClearDungeon();
                dungeonRebuildBuildAttempts++;

                dungeonBuildSuccesful = AttemptToBuildRandomDungeon(roomNodeGraph);
            }
            if(dungeonBuildSuccesful)
            {
                Debug.Log("Dungeon Built");
                InitiateRoomGameobjects();
            }
            
        }
        Debug.Log("Dungeon Not built");
        return dungeonBuildSuccesful;
    }
    private void LoadRoomTemplateIntoDictionary()
    {
        roomTemplateDictionary.Clear();

        // Load Room Temp List Into Dictionary
        foreach(RoomTemplateSO roomTemplate in roomTemplateList)
        {
            if (!roomTemplateDictionary.ContainsKey(roomTemplate.guid))
            {
                roomTemplateDictionary.Add(roomTemplate.guid, roomTemplate);
            }
            else
            {
                Debug.Log("Duplicate Room Template Key in " + roomTemplateList);
            }
        }
    }

    private bool AttemptToBuildRandomDungeon(RoomNodeGraphSO roomNodeGraph)
    {
        Queue<RoomNodeSO> openRoomNodeQueue = new Queue<RoomNodeSO>();
        RoomNodeSO entranceNode = roomNodeGraph.GetRoomNode(roomNodeTypeList.list.Find(x => x.isEntrance));

        if(entranceNode != null)
        {
            openRoomNodeQueue.Enqueue(entranceNode);
        }
        else
        {
            Debug.Log("No Entrance Node");
            return false;
        }
        
        bool noRoomOverlaps = true;
        noRoomOverlaps = ProcessRoomsInOpenRoomNodeQueue(roomNodeGraph, openRoomNodeQueue,noRoomOverlaps);
        if (openRoomNodeQueue.Count == 0 && noRoomOverlaps)
        { return true; }
        else
        { return false; }

    }

    private bool ProcessRoomsInOpenRoomNodeQueue(RoomNodeGraphSO roomNodeGraph, Queue<RoomNodeSO> openRoomNodeQueue, bool noRoomOverlaps)
    {
        while(openRoomNodeQueue.Count > 0 && noRoomOverlaps)
        {
            RoomNodeSO roomNode = openRoomNodeQueue.Dequeue();
            foreach(RoomNodeSO childRoomNode in roomNodeGraph.GetChildRoomNodes(roomNode))
            {
                openRoomNodeQueue.Enqueue(childRoomNode);
            }
            if(roomNode.roomNodeType.isEntrance) 
            {
                RoomTemplateSO roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);
                Room room  = CreateRoomFromRoomTemplate(roomTemplate,roomNode);
                room.isPositioned = true;

                dungeonBuilderRoomDictionary.Add(room.id,room);
            }
            else
            {
                Room parentRoom = dungeonBuilderRoomDictionary[roomNode.parentRoomNodeIDlist[0]];
                noRoomOverlaps = CanPlaceRoomWithNoOverlaps(roomNode, parentRoom);
            }
        }
        return noRoomOverlaps;
    }

    private bool CanPlaceRoomWithNoOverlaps(RoomNodeSO roomNode, Room parentRoom)
    {
        bool roomOverlaps = true;

        while (roomOverlaps) 
        { 
            List<Doorway> unconnectedAvailableParentDoorways = GetUnconnectedAvailableDoorways(parentRoom.doorWayList).ToList();
            if(unconnectedAvailableParentDoorways.Count == 0)
            {
                return false;
            }
            
            Doorway doorwayParent = unconnectedAvailableParentDoorways[UnityEngine.Random.Range(0,unconnectedAvailableParentDoorways.Count)];

  //To get correct orientation of template(tilemap)..like if corridor then EW corridor or NS corridor..dont have to worry about orientation if roomtype not corridor
            RoomTemplateSO roomTemplate = GetRandomTemplateForRoomConsistentWithParent(roomNode,doorwayParent);

            Room room = CreateRoomFromRoomTemplate(roomTemplate,roomNode);

            if (PlaceTheRoom(parentRoom, doorwayParent, room))
            {
                roomOverlaps = false;
                room.isPositioned = true;
                dungeonBuilderRoomDictionary.Add(room.id, room);
            }
            else { roomOverlaps = true; }
        }
        return true; // no room overlaps
    }

    private RoomTemplateSO GetRandomTemplateForRoomConsistentWithParent(RoomNodeSO roomNode,Doorway doorwayParent)
    {
        RoomTemplateSO roomTemplate = null;
        if(roomNode.roomNodeType.isCorridor)
        {
            switch(doorwayParent.orientation)
            {
                case Orientations.north:
                case Orientations.south:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorNS));
                    break;
                case Orientations.east:
                case Orientations.west:
                    roomTemplate = GetRandomRoomTemplate(roomNodeTypeList.list.Find(x => x.isCorridorEW));
                    break;
                case Orientations.none:
                    break;
                default:
                    break;
            }
        }
        else
        {
            roomTemplate = GetRandomRoomTemplate(roomNode.roomNodeType);
        }
        return roomTemplate;
    }

    private bool PlaceTheRoom(Room parentRoom, Doorway doorwayParent, Room room)
    {
        Doorway doorway = GetOppositeDoorway(doorwayParent, room.doorWayList);
        if (doorway == null)
        {
            // mark doorway unavailable so dont try to connect to it again
            doorwayParent.isUnavailable = true;

            return false;
        }

        Vector2Int parentDoorwayPosition = parentRoom.lowerBounds + doorwayParent.position - parentRoom.templateLowerBounds;
        Vector2Int adjustment = Vector2Int.zero;

        switch (doorway.orientation)
        {
            case Orientations.east:
                adjustment = new Vector2Int(-1, 0);
                break;
            case Orientations.west:
                adjustment = new Vector2Int(1, 0);
                break;
            case Orientations.north:
                adjustment = new Vector2Int(0, -1);
                break;
            case Orientations.south:
                adjustment = new Vector2Int(0, 1);
                break;
            case Orientations.none:
                break;
            default:
                break;
        }
        room.lowerBounds = parentDoorwayPosition + adjustment + room.templateLowerBounds - doorway.position;
        room.upperBounds = parentDoorwayPosition + room.templateUpperBounds - room.templateLowerBounds;

        Room overlappingRoom = CheckForRoomOverlap(room);
        if (overlappingRoom == null) 
        {
            doorwayParent.isConnected = true;
            doorwayParent.isUnavailable = true;

            doorway.isConnected = true;
            doorway.isUnavailable = true;

            return true;
        }
        else
        {
            doorwayParent.isUnavailable = true;
            return false;
        }
            
    }

    private Doorway GetOppositeDoorway(Doorway parentDoorway,List<Doorway> doorWayList)
    {
        foreach(Doorway doorwayToCheck in doorWayList)
        {
            if(parentDoorway.orientation == Orientations.east && doorwayToCheck.orientation == Orientations.west) 
            {
                return doorwayToCheck;
            }
            else if (parentDoorway.orientation == Orientations.west && doorwayToCheck.orientation == Orientations.east)
            {
                return doorwayToCheck;
            }
            else if (parentDoorway.orientation == Orientations.north && doorwayToCheck.orientation == Orientations.south)
            {
                return doorwayToCheck;
            }
            else if (parentDoorway.orientation == Orientations.south && doorwayToCheck.orientation == Orientations.north)
            {
                return doorwayToCheck;
            }
        }
        return null;
    }

    private Room CheckForRoomOverlap(Room roomToTest)
    {
        // To check if roomTotest overlaps with any other room placed earlier...so loop will check for every room
        foreach(KeyValuePair<string, Room> keyValuePair in dungeonBuilderRoomDictionary) 
        {
            Room room = keyValuePair.Value;

            if(room.id == roomToTest.id || !room.isPositioned)
            {
                continue;
            }
            if(IsOverlappingRoom(roomToTest, room))
            {
                return room;
            }
        }
        return null; 
    }

    private bool IsOverlappingRoom(Room room1,Room room2)
    {
        bool isOverlappingX = IsOverLappingInterval(room1.lowerBounds.x, room1.upperBounds.x,room2.lowerBounds.x,room2.upperBounds.x);
        bool isOverlappingY = IsOverLappingInterval(room1.lowerBounds.y, room1.upperBounds.y, room2.lowerBounds.y, room2.upperBounds.y);

        if(isOverlappingX && isOverlappingY)
        {
            return true;
        }
        return false;
    }

    private bool IsOverLappingInterval(int min1, int max1, int min2, int max2)
    {
        if (Mathf.Max(min1, min2) <= Mathf.Max(max1, max2)) { return true; }
        return false; 
    }


    // From list of all templates..returns the ones matching with the roomNodetype needed
    private RoomTemplateSO GetRandomRoomTemplate(RoomNodeTypeSO roomNodeType)
    {
        List<RoomTemplateSO> matchingRoomTemplateList = new List<RoomTemplateSO>();
        foreach(RoomTemplateSO roomTemplate in roomTemplateList)
        {
            if(roomTemplate.roomNodeType == roomNodeType)
            {
                matchingRoomTemplateList.Add(roomTemplate);
            }
        }
        if(matchingRoomTemplateList.Count == 0) { return null; }
        return matchingRoomTemplateList[UnityEngine.Random.Range(0, matchingRoomTemplateList.Count)];
    }

    private IEnumerable<Doorway> GetUnconnectedAvailableDoorways(List<Doorway> roomDoorwayList)
    {
        foreach(Doorway doorway in roomDoorwayList)
        {
            if(!doorway.isConnected && !doorway.isUnavailable) 
            { 
                yield return doorway; 
            }
        }
    }
    private Room CreateRoomFromRoomTemplate(RoomTemplateSO roomTemplate,RoomNodeSO roomNode)
    {
        Room room = new Room();

        room.templateID = roomTemplate.guid;
        room.id = roomNode.id;
        room.prefab = roomTemplate.prefab;
        room.roomNodeType = roomTemplate.roomNodeType;
        room.lowerBounds = roomTemplate.lowerBounds;
        room.upperBounds = roomTemplate.upperBounds;
        room.spawnPositionArray = roomTemplate.spawnPositionArray;
        room.templateLowerBounds = roomTemplate.lowerBounds;
        room.templateUpperBounds = roomTemplate.upperBounds;
        room.childRoomIDList = CopyStringList(roomNode.childrenRoomNodeIDlist);
        room.doorWayList = CopyDoorwayList(roomTemplate.doorwayList);

        if(roomNode.parentRoomNodeIDlist.Count == 0)
        {
            room.parentRoomID = "";
            room.isPreviouslyVisited = true;
        }
        else 
        { 
            room.parentRoomID = roomNode.parentRoomNodeIDlist[0]; 
        }
        return room;
    }

    private RoomNodeGraphSO SelectRandomRoomNodeGraph(List<RoomNodeGraphSO> roomNodeGraphList)
    {
        if(roomNodeGraphList.Count > 0)
        { 
            return roomNodeGraphList[UnityEngine.Random.Range(0,roomNodeGraphList.Count)];
        }
        else 
        {
            Debug.Log("No Room node graphs in list");
            return null;
        }
    }

    private List<Doorway> CopyDoorwayList(List<Doorway> oldDoorwayList)
    { 
        List<Doorway> newDoorwayList = new List<Doorway>();

        foreach(Doorway doorway in oldDoorwayList)
        {
            Doorway newDoorway = new Doorway();

            newDoorway.position = doorway.position;
            newDoorway.orientation = doorway.orientation;
            newDoorway.doorPrefab = doorway.doorPrefab;
            newDoorway.isConnected = doorway.isConnected;
            newDoorway.isUnavailable = doorway.isUnavailable;
            newDoorway.doorwayStartCopyPosition = doorway.doorwayStartCopyPosition;
            newDoorway.doorwayCopyTileHeight = doorway.doorwayCopyTileHeight;
            newDoorway.doorwayCopyTileWidth = doorway.doorwayCopyTileWidth;

            newDoorwayList.Add(newDoorway);
        }
        return newDoorwayList;
    }


    private List<string> CopyStringList(List<string> oldStringlist)
    {
        List<string> newStringlist = new List<string>();
        foreach (string str in oldStringlist)
        {
            newStringlist.Add(str);
        }
        return newStringlist;
    }

    private void InitiateRoomGameobjects()
    {
        foreach(KeyValuePair<string,Room> keyvaluePair in dungeonBuilderRoomDictionary)
        {
            Room room = keyvaluePair.Value;

            Vector3 roomPosition = new Vector3(room.lowerBounds.x - room.templateLowerBounds.x, room.lowerBounds.y - room.templateLowerBounds.y, 0f);

            // "Instantiate" function just clones the tilemap prefab for the respective room to the respective position
            GameObject roomGameobject = Instantiate(room.prefab,roomPosition,Quaternion.identity,transform);

            InstantiatedRoom instantiatedRoom = roomGameobject.GetComponentInChildren<InstantiatedRoom>();
            instantiatedRoom.room = room;
            instantiatedRoom.Intialise(roomGameobject);
            room.instantiatedRoom = instantiatedRoom;
        }
    }

    public RoomTemplateSO GetRoomTemplate(string roomTemplateID)
    {
        if (roomTemplateDictionary.TryGetValue(roomTemplateID, out RoomTemplateSO roomTemplate)) { return roomTemplate; }
        else { return null; }
    }

    public Room GetRoomByRoomID(string roomID)
    {
        if (dungeonBuilderRoomDictionary.TryGetValue(roomID, out Room room)) { return room; }
        else { return null; }
    }


    private void ClearDungeon()
    {
        if(dungeonBuilderRoomDictionary.Count > 0)
        {
            foreach(KeyValuePair<string,Room> keyvaluepair in dungeonBuilderRoomDictionary)
            {
                Room room = keyvaluepair.Value;
                if(room.instantiatedRoom != null)
                {
                    Destroy(room.instantiatedRoom.gameObject);
                }
            }
            dungeonBuilderRoomDictionary.Clear();
        }
    }
}
