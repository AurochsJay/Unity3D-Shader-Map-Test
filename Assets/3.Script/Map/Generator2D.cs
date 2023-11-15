using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

public class Generator2D : MonoBehaviour
{
    enum CellType
    {
        None,
        Room,
        Hallway
    }

    enum CellDirection
    {
        right,
        left,
        up,
        down,
        none
    }

    enum HallwayCellPrefab
    {
        right,
        left,
        top
    }


    class Room
    {
        public RectInt bounds;

        public Room(Vector2Int location, Vector2Int size)
        {
            bounds = new RectInt(location, size);
        }

        public static bool Intersect(Room a, Room b)
        {
            return !((a.bounds.position.x -2 >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x -2)
                || (a.bounds.position.y -2 >= (b.bounds.position.y + b.bounds.size.y)) || (a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y -2);
            //a.bounds.position.x : a방의 왼쪽경계, b.bounds.position.x + b.bounds.size.x : b 방의 오른쪽경계.
            //a방의 왼쪽경계가 b방의 오른쪽 경계보다 크거나 같다면 두 방은 겹치지 않는다.
        }
        /* RectInt의 position 변수 -> minx와 miny를 뱉는다.
          public Vector2Int position 
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2Int(m_XMin, m_YMin); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x; m_YMin = value.y; }
        }
         */
    }

    [Header("던전 생성 변수")]
    [SerializeField] Vector2Int size;
    [SerializeField] int roomCount;
    [SerializeField] Vector2Int roomMaxSize;

    [Header("프리펩")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject parentPrefab; // 정리하기 위한 프리펩
    [SerializeField] private GameObject cornerTopRightPrefab;
    [SerializeField] private GameObject cornerTopLeftPrefab;
    [SerializeField] private GameObject cornerBottomRightPrefab;
    [SerializeField] private GameObject cornerBottomLeftPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject[] wallTopPrefabs;
    [SerializeField] private GameObject[] wallBottomPrefabs;
    [SerializeField] private GameObject[] wallRightPrefabs;
    [SerializeField] private GameObject[] wallLeftPrefabs;
    [SerializeField] private GameObject[] hallwayXPrefabs;
    [SerializeField] private GameObject[] hallwayYPrefabs;
    [SerializeField] private GameObject hallwayCornerTRPrefab;
    [SerializeField] private GameObject hallwayCornerTLPrefab;
    [SerializeField] private GameObject hallwayCornerBRPrefab;
    [SerializeField] private GameObject hallwayCornerBLPrefab;
    [SerializeField] private GameObject hallwayCrossPrefab;
    [SerializeField] private GameObject roomCeilingPrefab;
    [SerializeField] private GameObject dungeonEntrancePrefab;
    [SerializeField] Material redMaterial;
    [SerializeField] Material blueMaterial;
    
    Random random;
    Grid2D<CellType> grid;
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;

    //방 번호
    private int count = 1;
    //통로 번호
    private int countHall = 1;

    //마을에서 던전 입구까지 통로를 만들기 위한 변수
    Grid2D<CellType> gridEntrance;
    Vector2Int sizeEntrance;
    Vector2Int entrancePosition;
    private bool canUseDungeonEntrance = false; // 마을-던전입구 한번만 사용

    //보스방 포지션
    Vector2 bossRoomPosition = Vector2.zero;


    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        random = new Random(); // Random에 시드값을 넣는 이유는 맵이 계속 바뀌게 하지 않을려고
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        sizeEntrance = new Vector2Int(size.x, 5);
        gridEntrance = new Grid2D<CellType>(sizeEntrance, Vector2Int.zero);

        //방생성
        PlaceRooms();
        //들로네 삼각분할
        Triangulate();
        //모든복도생성
        CreateHallways();
        //사용할 복도만 생성
        PathfindHallways();
        //마을에서 던전입구로
        PathfindDungeonGate();
        //보스방 생성
        CreateBossRoom();
        

    }

    private void PlaceRooms()
    {
        Vector2Int gatePosition = size; // 가장 큰 값의 포지션

        for (int i =0; i<roomCount;i++)
        {
            Vector2Int location = new Vector2Int(random.Next(0, size.x), random.Next(0, size.y));
            Vector2Int roomSize = new Vector2Int(random.Next(6, roomMaxSize.x + 1), random.Next(6, roomMaxSize.y + 1));

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

            //방이 다 생성되고 나서 가장 x,z가 가장 작은 포지션을 가진 방을 찾아야한다.
            

            foreach (Room room in rooms)
            {
                if(Room.Intersect(room, buffer))
                {
                    Debug.Log("Intersect가 들어와서 add가 안됨");
                    add = false;
                    break;
                }
            }

            if(newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y)
            {
                Debug.Log("그리드 범위 밖으로 나간 큐브가 있는지");
                add = false;
            }

            if(add)
            {
                if(newRoom.bounds.size.x > 3 && newRoom.bounds.size.y > 3)
                {
                    Debug.Log("방이 추가됨");
                    rooms.Add(newRoom);
                    PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);

                    //방 뚜껑 씌우기 Ceiling
                    CreateRoomCeiling(newRoom);

                    foreach (var pos in newRoom.bounds.allPositionsWithin)
                    {
                        grid[pos] = CellType.Room;
                    }

                    //마을-던전 통로를 만들 방 선택
                    if(gatePosition.y > newRoom.bounds.position.y)
                    {
                        gatePosition = newRoom.bounds.position;

                        entrancePosition.x = (int)((newRoom.bounds.xMin + newRoom.bounds.xMax) * 0.5f);
                        entrancePosition.y = newRoom.bounds.yMin;
                        //Debug.Log("newRoom의 위치 : " + newRoom.bounds.position);
                        //Debug.Log("던전 입구의 위치 : " + entrancePosition);

                        //y 포지션이 같을 경우에 x가 더 작은게 문위치
                        if (gatePosition.x > newRoom.bounds.position.x)
                        {
                            gatePosition.x = newRoom.bounds.position.x;
                            entrancePosition.x = (int)((newRoom.bounds.xMin + newRoom.bounds.xMax) * 0.5f);
                        }
                    }

                    //보스방 찾고 만들기
                    FindBossRoom(newRoom);
                }
            }
        }
    }

    private void Triangulate()
    {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms)
        {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);

        //확인용도
        for (int i = 0; i < delaunay.Vertices.Count; i++)
        {
            Vector3 CheckPoint = new Vector3(delaunay.Vertices[i].Position.x, 0, delaunay.Vertices[i].Position.y);
            //Debug.DrawRay(CheckPoint, Vector3.up * 8f, Color.red, Mathf.Infinity);
            //Debug.Log(delaunay.Vertices[i].Position);
        }

        Debug.Log("엣지가 없는건가?" + delaunay.Edges.Count);
        for (int i = 0; i < delaunay.Edges.Count; i++)
        {
            //Debug.Log("여기로 들어옴" + i);
            //Debug.Log("???? 11: " + delaunay.Edges[i].U.Position);
        }
    }

    private void CreateHallways()
    {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        //Debug.Log("???? 11: " + delaunay.Edges[0].U.Position);
        foreach (var edge in delaunay.Edges)
        {

            edges.Add(new Prim.Edge(edge.U, edge.V));
            //Debug.Log("???? 11: " + edge.U.Position);
            //Debug.Log("???? 22: " + edge.V.Position);
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U); //리스트와 시작점(0)

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        //점들끼리 연결된 것을 보여주기
        //StartCoroutine(ShowLine(edges));

        //Debug.DrawRay(remainingEdges[0].edg, Vector3.up * 3f, Color.blue, Mathf.Infinity);

        foreach (var edge in remainingEdges)
        {
            Vector3 checkEdgePosition_U = new Vector3(edge.U.Position.x, 0, edge.U.Position.y);
            Vector3 checkEdgePosition_V = new Vector3(edge.V.Position.x, 0, edge.V.Position.y);
            
            //엣지 선이 어떻게 연결되었는지 Edge(U,V)
            //Debug.DrawLine(checkEdgePosition_U, checkEdgePosition_V, Color.blue, Mathf.Infinity);

            if (random.NextDouble() < 0.125) // 버린다는 건가? 특정 엣지들을 고르는 건 알겠다.
                                             // 문제는 왜 고르는거지? 모든 통로를 만드는건 비효율적이야
                                             // 근데 랜덤한 식으로 통로를 구하면 어떤 방은 못가지 않나?
            {
                selectedEdges.Add(edge);

                //if문안에서 실행되는거니까 저 범위안에 있는 녀석들만 들어와서 실행되겠지.
                //Debug.DrawLine(checkEdgePosition_U, checkEdgePosition_V, Color.green, Mathf.Infinity);
                //Debug.Log("SelectedEdges : " + edge.ToString());
                //Debug.DrawRay(checkEdgePosition_U, Vector3.up * 13f, Color.black, Mathf.Infinity);
                //Debug.DrawRay(checkEdgePosition_V, Vector3.up * 15f, Color.white, Mathf.Infinity);
                
            }
        }
    }

    private void PathfindHallways()
    {
        Pathfinder2D aStar = new Pathfinder2D(size);

        foreach (var edge in selectedEdges)
        {
            /*
             C#에서 as 키워드는 참조 형식을 다른 참조 형식으로 변환할 때 사용됩니다. 
             as 키워드는 명시적으로 형변환을 시도하고, 실패하면 null을 반환합니다.

             edge.U를 Vertex<Room> 형식으로 형변환하려 시도하고, 
            성공하면 해당 Vertex<Room> 객체의 Item 속성에 접근하여 startRoom 변수에 할당합니다. 
            만약 edge.U가 Vertex<Room> 형식이 아니라면 null이 startRoom에 할당됩니다.
             */
            var startRoom = (edge.U as Vertex<Room>).Item; // Vertex U가 edge의 시작점
            var endRoom = (edge.V as Vertex<Room>).Item; // Vertex V가 edge의 도착점

            var startPosf = startRoom.bounds.center; //임시로 부동소수점(float)으로 중앙값 받아옴
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            Vector3 startpositon = new Vector3(startPos.x, 1.6f, startPos.y);
            Vector3 endpositon = new Vector3(endPos.x, 1.6f, endPos.y);
            Debug.DrawLine(startpositon, endpositon, Color.red, Mathf.Infinity);
            Debug.DrawRay(startpositon, Vector3.up*3f, Color.green, Mathf.Infinity);
            Debug.DrawRay(endpositon, Vector3.up*2f, Color.blue, Mathf.Infinity);

            Debug.Log("selectedEdges의 개수 : " + selectedEdges.Count);

            //복도 부모오브젝트 (하이어아키 정리)
            GameObject parent = Instantiate(parentPrefab, Vector3.zero, Quaternion.identity);
            parent.transform.name = "HallWay " + countHall + "번";

            //b는 현재 탐색중인 이웃 노드를 나타낸다. a는 기준노드겠지
            /*
             costFunction은 FindPath 함수에서 호출될 때, 현재 노드인 a와 이웃 노드인 b의 정보가 자동으로 전달됩니다. 
            이때 a는 현재 탐색 중인 노드의 위치를 나타냅니다. 
            따라서 a.Position은 현재 탐색 중인 노드의 위치가 됩니다.

            이렇게 a가 시작 노드의 위치를 나타내는 것은 FindPath 함수 내부에서 시작 노드의 정보를 초기화하고, 우선순위 큐에 추가할 때 결정됩니다. 
            시작 노드의 정보를 초기화한 후에 우선순위 큐에 넣을 때, a.Position에 해당하는 위치 정보를 사용하여 시작 노드의 위치를 설정합니다. 
            그러면 이후 costFunction에서 a가 시작 노드를 나타내게 됩니다.
             */
            var path = aStar.FindPath(startPos, endPos, (Pathfinder2D.Node a, Pathfinder2D.Node b) =>
            {
                var pathCost = new Pathfinder2D.PathCost();

                pathCost.cost = Vector2Int.Distance(b.Position, endPos); // heuristic // b의 위치가 어디인지?

                if (grid[b.Position] == CellType.Room)
                {
                    pathCost.cost += 10;
                }
                else if (grid[b.Position] == CellType.None)
                {
                    pathCost.cost += 5;
                }
                else if (grid[b.Position] == CellType.Hallway)
                {
                    pathCost.cost += 1;
                }

                pathCost.traversable = true;

                return pathCost;
            }
            );


            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    var current = path[i];
                    //Debug.DrawRay(new Vector3(current.x, 0, current.y), Vector3.up * 10f, Color.cyan, Mathf.Infinity);

                    if (grid[current] == CellType.None)
                    {
                        grid[current] = CellType.Hallway; // 빈방이면 복도로 바꾼다.

                        
                        if(grid[path[i-1]] == CellType.Room)
                        {
                            Debug.DrawRay(new Vector3(path[i-1].x, 0, path[i-1].y), Vector3.up * 4f, Color.black, Mathf.Infinity);
                            Debug.Log("i-1번째(첫 통로방의 이전)는 방이다");

                            Vector3 prevPos = new Vector3(path[i - 1].x, 2f, path[i - 1].y);

                            //방(Room)의 출입구 지우고 다시 만들기.
                            RegenerateEntrance(prevPos, parent);
                        }

                        if(grid[path[i+1]] == CellType.Room)
                        {
                            Debug.DrawRay(new Vector3(path[i + 1].x, 0, path[i + 1].y), Vector3.up * 6f, Color.white, Mathf.Infinity);
                            Debug.Log("i+1번째(마지막 통로방의 다음)은 방이다");

                            Vector3 nextPos = new Vector3(path[i + 1].x, 2f, path[i + 1].y);

                            RegenerateEntrance(nextPos, parent);
                        }

                    }

                    if (i > 0 && i < path.Count-1)
                    {
                        // 이전방 -> 현재방 -> 다음방, delta가 (1,0)이면 오른쪽, (-1,0)이면 왼쪽, (0,1)이면 위, (0,-1)이면 아래
                        var prev = path[i - 1];

                        var next = path[i + 1];

                        var delta1 = current - prev;

                        var delta2 = next - current;

                        CellDirection delta1Dir;
                        CellDirection delta2Dir;

                        delta1Dir = CheckCellDirection(delta1);
                        delta2Dir = CheckCellDirection(delta2);

                        if(grid[current] == CellType.Hallway)
                        {
                            PlaceHallway(delta1Dir, delta2Dir, current, parent);
                        }
                    }

                }

                foreach (var pos in path)
                {
                    if (grid[pos] == CellType.Hallway)
                    {
                        //PlaceHallway(pos);
                    }
                }
            }
            countHall++;
        }
    }

    private void PathfindDungeonGate()
    {
        // 두개의 정점을 만들것임.

        Vertex vertexU = new Vertex(new Vector3(5, 0, 0));
        Vertex vertexV = new Vertex(new Vector3(entrancePosition.x, 0, entrancePosition.y));

        Edge edge = new Edge(vertexU, vertexV);

        Vector2Int startPos = new Vector2Int(10, 0);
        Vector2Int endPos = entrancePosition;

        gridEntrance[endPos] = CellType.Room;

        Debug.Log("얘는 무슨 값이지? Celltype을 갖고있나?" + gridEntrance[new Vector2Int(0, 0)]);
        Debug.Log("얘는 endpos위치에 Room을 넣었으니 Room이 나오겠지?" + gridEntrance[endPos]);

        Pathfinder2D aStar = new Pathfinder2D(sizeEntrance);

        GameObject parent = Instantiate(parentPrefab, Vector3.zero, Quaternion.identity);
        parent.transform.name = "마을-던전 통로";

        var path = aStar.FindPathToGate(startPos, endPos, (Pathfinder2D.Node a, Pathfinder2D.Node b) =>
        {
            var pathCost = new Pathfinder2D.PathCost();

            pathCost.cost = Vector2Int.Distance(b.Position, endPos); // heuristic // b의 위치가 어디인지?

            if (gridEntrance[b.Position] == CellType.Room)
            {
                pathCost.cost += 10;
            }
            else if (gridEntrance[b.Position] == CellType.None)
            {
                pathCost.cost += 5;
            }
            else if (gridEntrance[b.Position] == CellType.Hallway)
            {
                pathCost.cost += 1;
            }

            pathCost.traversable = true;

            return pathCost;
        }
        );

        if (path != null)
        {
            Debug.Log("새 그리드 path에 뭐가 담기나?");
            Debug.Log("새 그리드 path 몇개? " + path.Count);
            for (int i = 0; i < path.Count; i++)
            {
                var current = path[i];

                if (gridEntrance[current] == CellType.None)
                {
                    gridEntrance[current] = CellType.Hallway; // 빈방이면 복도로 바꾼다.


                    //if (gridEntrance[path[i - 1]] == CellType.Room)
                    //{
                    //    Vector3 prevPos = new Vector3(path[i - 1].x, 2f, path[i - 1].y);
                    //}

                    //if (gridEntrance[path[i + 1]] == CellType.Room)
                    //{
                    //    Vector3 nextPos = new Vector3(path[i + 1].x, 2f, path[i + 1].y);
                    //}

                }

                if (i > 0 && i < path.Count - 1)
                {
                    // 이전방 -> 현재방 -> 다음방, delta가 (1,0)이면 오른쪽, (-1,0)이면 왼쪽, (0,1)이면 위, (0,-1)이면 아래
                    var prev = path[i - 1];

                    var next = path[i + 1];

                    var delta1 = current - prev;

                    var delta2 = next - current;

                    CellDirection delta1Dir;
                    CellDirection delta2Dir;

                    delta1Dir = CheckCellDirection(delta1);
                    delta2Dir = CheckCellDirection(delta2);

                    if (gridEntrance[current] == CellType.Hallway)
                    {
                        PlaceHallway(delta1Dir, delta2Dir, current, parent);
                        Vector3 currentPos = new Vector3(current.x * 3, 2f, current.y * 3) - new Vector3(10f, 0, 80f);
                        Debug.DrawRay(currentPos, Vector3.up * 10f, Color.red, Mathf.Infinity);
                        //이 지점에서 던전입구를 가능하게 한다.
                        if(!canUseDungeonEntrance)
                        {
                            canUseDungeonEntrance = true;
                            DungeonEntrance(currentPos, delta1Dir, parent);
                        }

                        
                    }
                }

                //첫방 출입구 벽 제거
                if (i == path.Count - 2)
                {
                    Vector3 currentPos = new Vector3(current.x * 3, 2f, current.y * 3) - new Vector3(10f, 0, 80f);
                    DeleteFirstRoomWall(currentPos);
                }
            }
        }
    }

    //방의 지붕 만들기
    private void CreateRoomCeiling(Room newRoom)
    {
        Vector2 centerPosf = newRoom.bounds.center;
        Vector2Int centerPos = new Vector2Int((int)centerPosf.x, (int)centerPosf.y);

        Vector3 offset = new Vector3(-10f, 0, -80f);

        //Width 짝수 -> x : -1.5 변경, 홀수 -> 그대로
        if(newRoom.bounds.width % 2 == 0)
        {
            offset += new Vector3(-1.5f, 0, 0); 
        }
        
        //Height 짝수 -> z : -1.5 변경, 홀수 -> 그대로
        if(newRoom.bounds.height % 2 == 0)
        {
            offset += new Vector3(0, 0, -1.5f);
        }

        Vector3 centerPosition = new Vector3(centerPos.x * 3, 0.6f, centerPos.y * 3) + offset;
        //Vector3 centerPositionf = new Vector3(centerPosf.x * 3, 1f, centerPosf.y * 3) - new Vector3(10f, 0, 80f);
        Debug.DrawRay(centerPosition, Vector3.up * 15f, Color.blue, Mathf.Infinity);
        //Debug.DrawRay(centerPositionf, Vector3.up * 15f, Color.red, Mathf.Infinity);
        GameObject roomCeiling = Instantiate(roomCeilingPrefab, centerPosition, Quaternion.identity);
        roomCeiling.transform.localScale = new Vector3(newRoom.bounds.width*3, 9f, newRoom.bounds.height*3);
    }

    //보스방 만들기
    private void CreateBossRoom()
    {
        Vector3 bossRoomPos = new Vector3(bossRoomPosition.x, 6f, bossRoomPosition.y) + new Vector3(-10f,0,-80f);
        Debug.DrawRay(bossRoomPos, Vector3.up * 30f, Color.white, Mathf.Infinity);


    }

    //보스방 찾기
    private void FindBossRoom(Room newRoom)
    {
        if (bossRoomPosition.y <= newRoom.bounds.yMax)
        {
            bossRoomPosition.y = newRoom.bounds.center.y;

            //y 포지션이 같을 경우에 x가 더 큰게 보스위치
            if (bossRoomPosition.x < newRoom.bounds.position.x)
            {
                bossRoomPosition = newRoom.bounds.center;
            }

            bossRoomPosition.x = newRoom.bounds.center.x;
        }
    }


    private void PlaceCube(Vector2Int location, Vector2Int size)
    {
        //if(size.x <= 2 || size.y <=2) 이런식으로 사이즈가 얼마 이하면 생성하지 않게 할 수 있다.
        //자잘한 방을 쳐낸다.

        //조건에 따라서 방의 벽을 정한다.
        //코너이면, Corner -> Rotate y값 주고, BL(BottomLeft), BR(BottomRight), UL(UpLeft), UR(UpRight)
        //출입구면, Door
        //방의 경계벽이면, Wall

        //코너일 조건 -> BL : location.x, BR : location.x + size.x, UL : location.y, UR : location.y + size.y
        //출입구일 조건 -> 통로가 확보되어야함
        //방의 벽 -> 코너와 출입구, 내부를 제외한 모든 곳

        //방의 부모오브젝트 (하이어아키 정리하기 위해서)
        GameObject parent = Instantiate(parentPrefab, Vector3.zero, Quaternion.identity);
        parent.gameObject.name = "Room " + count + "번";

        

        if (size.x > 3 && size.y > 3)
        {
            //GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);
            //go.GetComponent<Transform>().localScale = new Vector3(size.x, 3, size.y); // 방의 local scale 방 크기를 정할 수 있다.
            //go.GetComponent<MeshRenderer>().material = material;

            //열
            for(int i = 0; i < size.y; i++)
            {
                //행
                for (int j = 0; j < size.x; j++)
                {
                    Vector3 createPos = new Vector3(location.x*3, 2f, location.y*3) + new Vector3(j*3, 0, i*3) - new Vector3(10f,0,80f);

                    int index;
                    int rand = random.Next(0, 10);

                    if (rand >= 0 && rand < 6)
                    {
                        index = 0; // 일반
                    }
                    else
                    {
                        index = 1; // 횃불
                    }

                    //코너 i=0,j=0 , i=0,j=size.x-1 , i=size.y-1,j=0 , i=size.y-1 j=size.x
                    if (i == 0 && j == 0)
                    {

                        GameObject cornerBL = Instantiate(cornerBottomLeftPrefab, createPos, Quaternion.identity);
                        cornerBL.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                        cornerBL.gameObject.name = "Prefab_" + count + "cornerBL";
                        cornerBL.transform.parent = parent.transform;
                        continue;
                    }
                    else if(i==0 && j == size.x-1)
                    {
                        GameObject cornerBR = Instantiate(cornerBottomRightPrefab, createPos, Quaternion.identity);
                        cornerBR.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                        cornerBR.gameObject.name = "Prefab_" + count + "cornerBR";
                        cornerBR.transform.parent = parent.transform;
                        continue;
                    }
                    else if (i == size.y -1 && j == 0)
                    {
                        GameObject cornerTL = Instantiate(cornerTopLeftPrefab, createPos, Quaternion.identity);
                        cornerTL.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                        cornerTL.gameObject.name = "Prefab_" + count + "cornerUL";
                        cornerTL.transform.parent = parent.transform;
                        continue;
                    }
                    else if (i == size.y-1 && j == size.x - 1)
                    {
                        GameObject cornerTR = Instantiate(cornerTopRightPrefab, createPos, Quaternion.identity);
                        cornerTR.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                        cornerTR.gameObject.name = "Prefab_" + count + "cornerUR";
                        cornerTR.transform.parent = parent.transform;
                        continue;
                    }
                    //벽
                    else if(i == 0 && (j != 0 || j != size.x-1))
                    {
                        GameObject wallBottom = Instantiate(wallBottomPrefabs[index], createPos, Quaternion.identity);
                        wallBottom.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                        wallBottom.gameObject.name = "Prefab_" + count + "wallBottom";
                        wallBottom.transform.parent = parent.transform;
                        continue;
                    }
                    else if(i == size.y -1 && (j != 0 || j != size.x - 1))
                    {
                        GameObject wallTop = Instantiate(wallTopPrefabs[index], createPos, Quaternion.identity);
                        wallTop.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                        wallTop.gameObject.name = "Prefab_" + count + "wallUp";
                        wallTop.transform.parent = parent.transform;
                        continue;
                    }
                    else if ((i != 0 || i != size.y - 1) && j == 0)
                    {
                        GameObject wallLeft = Instantiate(wallLeftPrefabs[index], createPos, Quaternion.identity);
                        wallLeft.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                        wallLeft.gameObject.name = "Prefab_" + count + "wallLeft";
                        wallLeft.transform.parent = parent.transform;
                        continue;
                    }
                    else if ((i != 0 || i != size.y - 1) && j == size.x - 1)
                    {
                        GameObject wallRight = Instantiate(wallRightPrefabs[index], createPos, Quaternion.identity);
                        wallRight.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                        wallRight.gameObject.name = "Prefab_" + count + "wallRight";
                        wallRight.transform.parent = parent.transform;
                        continue;
                    }


                    GameObject go = Instantiate(cubePrefab, createPos, Quaternion.identity);
                    go.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                    //go.gameObject.transform.localScale = new Vector3(1, 3, 1);
                    int index1 = size.x * i + j;
                    go.gameObject.name = "Prefab_" +count+" (" + index1 +")";
                    go.transform.parent = parent.transform;
                }
            }

            count++;
        }
        
        
        //방의 사이즈 size.x가 얼마일때 size.y가 얼마일 때
        //그에 해당하는 prefab을 만들면
        //지금 사용하고 있는 메서드를 PlaceRoom으로 두고 통로는 PlaceHallway를 써서 다르게 받으면 된다.
        //로컬 스케일을 건들지 말고 방이 생성된 위치 location에다가 너비와 높이를 곱해서 
        //타일 하나하나 생성하고 붙여만들면 되지 않을까?
        //for문을 써서 첫번째 index의 경우 벽을 만들고(방의 경계)
        //하나의 큐브에 

    }

    private void PlaceRoom(Vector2Int location, Vector2Int size)
    {
        PlaceCube(location, size);
    }

    //통로 생성
    private void PlaceHallway(CellDirection delta1Dir, CellDirection delta2Dir, Vector2Int current, GameObject parent)
    {
        Vector3 createPos = new Vector3(current.x*3, 2f, current.y*3) - new Vector3(10f,0,80f);

        //던전입구용 enum변수
        HallwayCellPrefab hallwaycell;

        //횃불 프리펩
        int index;
        int rand = random.Next(0, 10);

        if (rand >= 0 && rand < 6)
        {
            index = 0; // 일반
        }
        else
        {
            index = 1; // 횃불
        }

        if (delta1Dir == CellDirection.right)
        {
            if (delta2Dir == CellDirection.right) //오른쪽-오른쪽 : 일방향통로
            {
                //생성한다.
                GameObject Hallway_Straight = Instantiate(hallwayXPrefabs[index], createPos, Quaternion.identity);
                Hallway_Straight.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Straight.transform.parent = parent.transform;
            }
            else if (delta2Dir == CellDirection.up) // 오른쪽-위 : 꺾인 통로
            {
                GameObject Hallway_Corner = Instantiate(hallwayCornerBRPrefab, createPos, Quaternion.identity);
                Hallway_Corner.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Corner.transform.parent = parent.transform;
            }
            else if (delta2Dir == CellDirection.down) // 오른쪽-아래 : 꺾인 통로
            {
                GameObject Hallway_Corner = Instantiate(hallwayCornerTRPrefab, createPos, Quaternion.identity);
                Hallway_Corner.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Corner.transform.parent = parent.transform;
            }
        }
        else if (delta1Dir == CellDirection.left)
        {
            if (delta2Dir == CellDirection.left) // 왼쪽-왼쪽
            {
                GameObject Hallway_Straight = Instantiate(hallwayXPrefabs[index], createPos, Quaternion.identity);
                Hallway_Straight.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Straight.transform.parent = parent.transform;
            }
            else if (delta2Dir == CellDirection.up) // 왼쪽-위
            {
                GameObject Hallway_Corner = Instantiate(hallwayCornerBLPrefab, createPos, Quaternion.identity);
                Hallway_Corner.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Corner.transform.parent = parent.transform;
            }
            else if (delta2Dir == CellDirection.down) // 왼쪽-아래
            {
                GameObject Hallway_Corner = Instantiate(hallwayCornerTLPrefab, createPos, Quaternion.identity);
                Hallway_Corner.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Corner.transform.parent = parent.transform;
            }
        }
        else if (delta1Dir == CellDirection.up)
        {
            if (delta2Dir == CellDirection.right) // 위-오른쪽
            {
                GameObject Hallway_Corner = Instantiate(hallwayCornerTLPrefab, createPos, Quaternion.identity);
                Hallway_Corner.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Corner.transform.parent = parent.transform;
            }
            else if (delta2Dir == CellDirection.left) // 위-왼쪽
            {
                GameObject Hallway_Corner = Instantiate(hallwayCornerTRPrefab, createPos, Quaternion.identity);
                Hallway_Corner.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Corner.transform.parent = parent.transform;
            }
            else if (delta2Dir == CellDirection.up) // 위-위
            {
                GameObject Hallway_Straight = Instantiate(hallwayYPrefabs[index], createPos, Quaternion.identity);
                Hallway_Straight.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Straight.transform.parent = parent.transform;
            }
        }
        else if (delta1Dir == CellDirection.down)
        {
            if (delta2Dir == CellDirection.right) // 아래-오른쪽
            {
                GameObject Hallway_Corner = Instantiate(hallwayCornerBLPrefab, createPos, Quaternion.identity);
                Hallway_Corner.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Corner.transform.parent = parent.transform;
            }
            else if (delta2Dir == CellDirection.left) //아래-왼쪽
            {
                GameObject Hallway_Corner = Instantiate(hallwayCornerBRPrefab, createPos, Quaternion.identity);
                Hallway_Corner.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Corner.transform.parent = parent.transform;
            }
            else if (delta2Dir == CellDirection.down) // 아래-아래
            {
                GameObject Hallway_Straight = Instantiate(hallwayYPrefabs[index], createPos, Quaternion.identity);
                Hallway_Straight.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                Hallway_Straight.transform.parent = parent.transform;
            }
        }

        //두개의 복도 프리펩이 있는지 확인하고 두개라면 모든 방향으로 하나씩 제거. 두개인것은 위하고 아래, 또는 하나의 벽이다.
        CheckOverlapHallway(createPos);

        //위에 아무것도 없어야 겹치는 코너 ceiling 생성
        //CreateCross(createPos);
    }

    private CellDirection CheckCellDirection(Vector2Int delta)
    {
        CellDirection result;

        if(delta == Vector2Int.right)
        {
            result = CellDirection.right;
        }
        else if(delta == Vector2Int.left)
        {
            result = CellDirection.left;
        }
        else if(delta == Vector2Int.up)
        {
            result = CellDirection.up;
        }
        else if(delta == Vector2Int.down)
        {
            result = CellDirection.down;
        }
        else
        {
            result = CellDirection.none;
        }

        return result;
    }

    //방(Room)의 출입구 지우고 다시 만들기.
    private void RegenerateEntrance(Vector3 Pos, GameObject parent)
    {
        Pos = new Vector3(Pos.x *3, Pos.y, Pos.z *3) - new Vector3(10f,0,80f);

        DeleteCube(Pos);

        GameObject RoomEntrance = Instantiate(cubePrefab, Pos, Quaternion.identity);
        RoomEntrance.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
        RoomEntrance.transform.name = "RoomEntrance";
        RoomEntrance.transform.parent = parent.transform;
    }

    //복도 겹치는거 확인하고 제거
    private void CheckOverlapHallway(Vector3 checkPos)
    {
        int checkCount = 0;
        int checkCount_forward = 0;
        int checkCount_back = 0;
        int checkCount_right = 0;
        int checkCount_left = 0;

        //방향표시
        CellDirection checkCellDirection;

        //아래방향 겹치는거 제거
        Collider[] colliders = Physics.OverlapCapsule(checkPos, checkPos + Vector3.down * 0.5f, 0.1f);
        foreach(Collider collider in colliders)
        {
            checkCount++;
            if(checkCount >= 2)
            {
                Debug.Log("2개 이상 물체가 겹친데가 있다.");
                Debug.Log("colliders의 개수 : " + colliders.Length);
                DeleteCube(checkPos);
                Debug.Log(checkPos);
                

                //앞방향
                Collider[] colliders_forward = Physics.OverlapCapsule(checkPos, checkPos + Vector3.forward * 0.3f, 0.01f);
                foreach (Collider collider_forward in colliders_forward)
                {
                    checkCount_forward++;
                    if (checkCount_forward >= 2)
                    {
                        Debug.Log("포워드 들어옴, 위치 : " + checkPos);
                        checkCellDirection = CellDirection.up;
                        CreateCrossCube(checkPos, checkCellDirection);
                    }
                }

                //뒷방향
                Collider[] colliders_back = Physics.OverlapCapsule(checkPos, checkPos + Vector3.back * 0.3f, 0.01f);
                foreach (Collider collider_back in colliders_back)
                {
                    checkCount_back++;
                    if (checkCount_back >= 2)
                    {
                        Debug.Log("백 들어옴, 위치 : " + checkPos);
                        checkCellDirection = CellDirection.down;
                        CreateCrossCube(checkPos, checkCellDirection);
                    }
                }

                //오른쪽
                Collider[] colliders_right = Physics.OverlapCapsule(checkPos, checkPos + Vector3.right * 0.3f, 0.01f);
                foreach (Collider collider_right in colliders_right)
                {
                    checkCount_right++;
                    if (checkCount_right >= 2)
                    {
                        Debug.Log("라이트 들어옴, 위치 : " + checkPos);
                        checkCellDirection = CellDirection.right;
                        CreateCrossCube(checkPos, checkCellDirection);
                    }
                }

                //왼쪽
                Collider[] colliders_left = Physics.OverlapCapsule(checkPos, checkPos + Vector3.left * 0.3f, 0.01f);
                foreach (Collider collider_left in colliders_left)
                {
                    checkCount_left++;
                    if (checkCount_left >= 2)
                    {
                        Debug.Log("레프트 들어옴, 위치 : " + checkPos);
                        checkCellDirection = CellDirection.right;
                        CreateCrossCube(checkPos, checkCellDirection);
                    }
                }
            }
        }

    }

    //6방향 제거, up,down,right,left,forward,back
    private void DeleteCube(Vector3 Pos)
    {
        RaycastHit hit;
        RaycastHit[] hits;

        if (Physics.Raycast(Pos, Vector3.up, out hit, 10f))
        {
            Debug.Log("첫 레이에 부딪힌 녀석 위치 : " + hit.transform.position + "레이에 부딪힌 녀석 이름 : " + hit.transform.name);
            Destroy(hit.transform.gameObject);
            Debug.Log("y축 방향 위에 지웠다 : " + Pos);
            
        }

        //hallway 겹치는거 2개 다 제거
        hits = Physics.RaycastAll(Pos, Vector3.up, 10f);
        foreach(var hit_up in hits)
        {
            Debug.Log("hit_up 위치 : " + hit_up.transform.position + "hit_up 이름 : " + hit_up.transform.name);
            Destroy(hit_up.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.down, out hit, 1f))
        {
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.forward, out hit, 0.5f))
        {
            DestroyTorch(hit);
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.back, out hit, 0.5f))
        {
            DestroyTorch(hit);
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.right, out hit, 0.5f))
        {
            DestroyTorch(hit);
            Destroy(hit.transform.gameObject);
        }

        if (Physics.Raycast(Pos, Vector3.left, out hit, 0.5f))
        {
            DestroyTorch(hit);
            Destroy(hit.transform.gameObject);
        }
    }

    //겹치는 코너 생성
    private void CreateCrossCube(Vector3 createPos, CellDirection cellDirection)
    {
        Debug.DrawRay(createPos + new Vector3(0, 10f, 0), Vector3.down * 10f, Color.red, Mathf.Infinity);

        switch (cellDirection)
        {
            case CellDirection.up:
                GameObject hallwayCross_up = Instantiate(hallwayCrossPrefab, createPos, Quaternion.identity);
                hallwayCross_up.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                break;
            case CellDirection.down:
                GameObject hallwayCross_down = Instantiate(hallwayCrossPrefab, createPos, Quaternion.identity);
                hallwayCross_down.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                hallwayCross_down.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;
            case CellDirection.right:
                GameObject hallwayCross_right = Instantiate(hallwayCrossPrefab, createPos, Quaternion.identity);
                hallwayCross_right.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                hallwayCross_right.transform.localRotation = Quaternion.Euler(0, 90, 0);
                break;
            case CellDirection.left:
                GameObject hallwayCross_left = Instantiate(hallwayCrossPrefab, createPos, Quaternion.identity);
                hallwayCross_left.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                hallwayCross_left.transform.localRotation = Quaternion.Euler(0, 270, 0);
                break;
        }
    }

    //마을-던전입구 생성, down 밑에는 없다. 좌 우 위 3방향
    private void DungeonEntrance(Vector3 createPos, CellDirection cellDir, GameObject parent)
    {
        switch(cellDir)
        {
            case CellDirection.right:
                GameObject dungeonEntrance_right = Instantiate(dungeonEntrancePrefab, createPos, Quaternion.identity);
                dungeonEntrance_right.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                dungeonEntrance_right.transform.localRotation = Quaternion.Euler(0, 90, 0);
                dungeonEntrance_right.transform.parent = parent.transform;
                break;
            case CellDirection.left:
                GameObject dungeonEntrance_left = Instantiate(dungeonEntrancePrefab, createPos, Quaternion.identity);
                dungeonEntrance_left.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                dungeonEntrance_left.transform.localRotation = Quaternion.Euler(0, 270, 0);
                dungeonEntrance_left.transform.parent = parent.transform;
                break;
            case CellDirection.up:
                GameObject dungeonEntrance_top = Instantiate(dungeonEntrancePrefab, createPos, Quaternion.identity);
                dungeonEntrance_top.GetComponent<Transform>().localScale = new Vector3(3, 3f, 3);
                dungeonEntrance_top.transform.parent = parent.transform;
                break;
        }
    }

    //첫방 출입구 벽 제거
    private void DeleteFirstRoomWall(Vector3 currentPos)
    {
        Debug.DrawRay(currentPos, Vector3.up * 30f, Color.white, Mathf.Infinity);
        RaycastHit hit;
        
        if(Physics.Raycast(currentPos, Vector3.forward, out hit, 5f))
        {
            Debug.Log("DeleteFirstRoomWall if문 들어옴");
            Destroy(hit.transform.gameObject);
        }
    }

    //횃불제거
    private void DestroyTorch(RaycastHit hit)
    {
        Debug.Log("맞은 녀석의 자식 개수 : " + hit.transform.childCount);
        if(hit.transform.childCount > 0)
        {
            Debug.Log("첫번째 자식 이름? " + hit.transform.GetChild(0));
            if (hit.transform.GetChild(hit.transform.childCount - 1).transform.name == "Torch_01")
            {
                Debug.Log("지웠음, 위치는 ? " + hit.transform.GetChild(hit.transform.childCount -1).transform.position);
                Destroy(hit.transform.GetChild(hit.transform.childCount - 1).transform.gameObject);
            }
        }
    }

//잠시 DrawLine 볼수있게 코루틴
private IEnumerator ShowLine(List<Prim.Edge> edges)
    {
        for (int i = 1; i < edges.Count; i++)
        {

            Vector3 checkEdgePosition_U = new Vector3(edges[i - 1].U.Position.x, 0, edges[i - 1].U.Position.y);
            Vector3 checkEdgePosition_U2 = new Vector3(edges[i].U.Position.x, 0, edges[i].U.Position.y);
            Vector3 checkEdgePosition_V = new Vector3(edges[i - 1].V.Position.x, 0, edges[i - 1].V.Position.y);
            Vector3 checkEdgePosition_V2 = new Vector3(edges[i].V.Position.x, 0, edges[i].V.Position.y);

            Debug.Log("첫번째 U의 포지션 : " + checkEdgePosition_U);
            Debug.Log("두번째 U의 포지션 : " + checkEdgePosition_U2);

            Debug.DrawLine(checkEdgePosition_U, checkEdgePosition_U2, Color.green, Mathf.Infinity);
            Debug.DrawLine(checkEdgePosition_V, checkEdgePosition_V2, Color.blue, Mathf.Infinity);

            yield return new WaitForSeconds(0.3f);
        }
    }

}
