using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;

public class RoomNodeGraphEditor : EditorWindow
{
    private GUIStyle roomNodeStyle;
    private static RoomNodeGraphSO currentRoomNodeGraph;
    private RoomNodeSO currentRoomNode = null;
    private RoomNodeTypeListSO roomNodeTypeList;

    // Node layout values
    private const float nodeWith = 160f;
    private const float nodeHeight = 75f;
    private const int nodePadding = 25;
    private const int nodeBorder = 12;

    // Connecting line values
    private float connectingLineWidth = 3f;
    private float connectingLineArrowSize = 6f;

    [MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
    private static void OpenWindow()
    {
        GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
    }

    private void OnEnable()
    {
        // Define node layout style
        roomNodeStyle = new GUIStyle();
        roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
        roomNodeStyle.normal.textColor = Color.white;
        roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
        roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

        // Load Room node types
        roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
    }

    /// <summary>
    /// Open the room node graph editor window if a room node graph scriptable object asset is double clicked in the inspector
    /// </summary>
    [OnOpenAsset(0)]  // Need the namespace UnityEditor.Callbacks
    public static bool OnDoubleClickAsset(int instanceID, int line)
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

    /// <summary>
    /// Draw Editor Gui
    /// </summary>
    private void OnGUI()
    {
        // If a scriptable object of type RoomNodeGraphSO has been selected the process
        if (currentRoomNodeGraph != null)
        {
            // Draw line if being dragged
            DrawDraggedLines();

            // Process Events
            ProcessEvents(Event.current);

            //Draw Connections Between Room Nodes
            DrawRoomConnections();

            // Draw Room Nodes
            DrawRoomNodes();
        }

        if (GUI.changed)
            Repaint();
    }

    private void DrawDraggedLines()
    {
        if (currentRoomNodeGraph.linePosition != Vector2.zero)
        {
            // Draw line from node to line position
            Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition,
             currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, Color.white, null, connectingLineWidth);
        }
    }

    private void ProcessEvents(Event currentEvent)
    {
        // Get room node that mouse is over if it's null or not currently being dragged
        if (currentRoomNode == null || currentRoomNode.isLeftClickDragging == false)
        {
            currentRoomNode = IsMouseOverRoomNode(currentEvent);
        }

        // if mouse isn't over a room node or we are currently dragging a line from the room node then process graph events
        if (currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            ProcessRoomNodeGraphEvents(currentEvent);
        }
        else // else process room node events
        {
            // process room node events
            currentRoomNode.ProcessEvents(currentEvent);
        }
    }

    /// <summary>
    /// Check to see to mouse is over a room node - if so then return the room node else return null
    /// </summary>
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

    /// <summary>
    /// Process Room Node Graph Events
    /// </summary>
    private void ProcessRoomNodeGraphEvents(Event currentEvent)
    {
        switch (currentEvent.type)
        {
            // Process Mouse Down Events
            case EventType.MouseDown:
                ProcessMouseDownEvent(currentEvent);
                break;

            // Process Mouse Up Events
            case EventType.MouseUp:
                ProcessMouseUpEvent(currentEvent);
                break;

            // Process Mouse Drag Events
            case EventType.MouseDrag:
                ProcessMouseDragEvent(currentEvent);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Process mouse down events on the room node graph (not over a node)
    /// </summary>
    private void ProcessMouseDownEvent(Event currentEvent)
    {
        // Process right click mouse down on graph event (show context menu)
        if (currentEvent.button == 1)
        {
            ShowContextMenu(currentEvent.mousePosition);
        }
    }

    /// <summary>
    /// Show the context menu
    /// </summary>
    private void ShowContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);

        menu.ShowAsContext();
    }

    /// <summary>
    /// Create a room node at the mouse position
    /// </summary>
    private void CreateRoomNode(object mousePositionObject)
    {
        CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
    }

    /// <summary>
    /// Create a room node at the mouse position - overloaded to also pass in the RoomNodeType
    /// </summary>
    private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
    {
        Vector2 mousePosition = (Vector2)mousePositionObject;

        // create room node scriptable object asset
        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

        // add room node to current room node graph room node list
        currentRoomNodeGraph.roomNodeList.Add(roomNode);

        // set room node values
        roomNode.Initialize(new Rect(mousePosition.x, mousePosition.y, nodeWith, nodeHeight), currentRoomNodeGraph, roomNodeType);

        // add room node to room node graph scriptable object asset database
        AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);

        AssetDatabase.SaveAssets();

        // Refresh graph node dictionary
        currentRoomNodeGraph.OnValidate();
    }

    /// <summary>
    /// Process mouse up events
    /// </summary>
    private void ProcessMouseUpEvent(Event currentEvent)
    {
        // if releasing the right mouse button and currently dragging a line
        if (currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            // Check if mouse is over a room node
            RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);

            if (roomNode != null)
            {
                // if so set it as a child of the parent room node if it can be added
                if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeIDToRoomNode(roomNode.id))
                {
                    // Set parent ID in child room node
                    roomNode.AddParentRoomNodeIDToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
                }
            }

            ClearLineDrag();
        }
    }

    /// <summary>
    /// Process mouse drag events
    /// </summary>
    private void ProcessMouseDragEvent(Event currentEvent)
    {
        // process right click drag event - draw line
        if (currentEvent.button == 1)
        {
            ProcessRightMouseDragEvent(currentEvent);
        }
    }

    /// <summary>
    /// Process right mouse drag event - draw line
    /// </summary>
    private void ProcessRightMouseDragEvent(Event currentEvent)
    {
        if (currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
        {
            DragConnectingLine(currentEvent.delta);
            GUI.changed = true;
        }
    }

    /// <summary>
    /// Drag connecting line from room node
    /// </summary>
    private void DragConnectingLine(Vector2 delta)
    {
        currentRoomNodeGraph.linePosition += delta;
    }

    /// <summary>
    /// Clear line drag from a room node
    /// </summary>
    private void ClearLineDrag()
    {
        currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
        currentRoomNodeGraph.linePosition = Vector2.zero;
        GUI.changed = true;
    }

    /// <summary>
    /// Draw connections int the graph window between room nodes
    /// </summary>
    private void DrawRoomConnections()
    {
        // Loop through all room nodes
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            if (roomNode.childRoomNodeIDList.Count > 0)
            {
                // Loop through child room nodes
                foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
                {
                    // get child room node from dictionary
                    if (currentRoomNodeGraph.roomNodeDictionary.TryGetValue(childRoomNodeID, out RoomNodeSO childRoomNode))
                    {
                        DrawConnectionLine(roomNode, childRoomNode);

                        GUI.changed = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Draw connection line between the parent room node and child room node
    /// </summary>
    private void DrawConnectionLine(RoomNodeSO roomNode, RoomNodeSO childRoomNode)
    {
        // get line start and end positions
        Vector2 startPosition = roomNode.rect.center;
        Vector2 endPosition = childRoomNode.rect.center;

        // calculate midway point
        Vector2 midPosition = (startPosition + endPosition) / 2f;

        // Vector from start end position of line
        Vector2 direction = endPosition - startPosition;

        // Calculate normalized perpendicular positions from the mid point
        Vector2 arrowTailPoint1 = midPosition - new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
        Vector2 arrowTailPoint2 = midPosition + new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;

        // Calculate mid point offset position for arrow head
        Vector2 arrowHeadPoint = midPosition + direction.normalized * connectingLineArrowSize;

        // Draw Arrow
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, Color.white, null, connectingLineWidth);
        Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, Color.white, null, connectingLineWidth);

        // Draw line
        Handles.DrawBezier(startPosition, endPosition, startPosition, endPosition, Color.white, null, connectingLineWidth);

        GUI.changed = true;
    }


    /// <summary>
    /// Draw room nodes in the graph window
    /// </summary>
    private void DrawRoomNodes()
    {
        // Loop through all room nodes and draw them
        foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
        {
            roomNode.Draw(roomNodeStyle);
        }

        GUI.changed = true;
    }
}
