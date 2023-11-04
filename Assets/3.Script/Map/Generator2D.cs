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

    class Room
    {
        public RectInt bounds;

        public Room(Vector2Int location, Vector2Int size)
        {
            bounds = new RectInt(location, size);
        }

        public static bool Intersect(Room a, Room b)
        {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
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

    [SerializeField] Vector2Int size;
    [SerializeField] int roomCount;
    [SerializeField] Vector2Int roomMaxSize;
    [SerializeField] GameObject cubePrefab;
    [SerializeField] private GameObject cornerPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] Material redMaterial;
    [SerializeField] Material blueMaterial;

    Random random;
    Grid2D<CellType> grid;
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;


    //방 번호
    private int count = 1;
    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        random = new Random(0); // Random에 시드값을 넣는 이유는 맵이 계속 바뀌게 하지 않을려고
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        //방생성
        PlaceRooms();
        //들로네 삼각분할
        Triangulate();
        //모든복도생성
        CreateHallways();
        //사용할 복도만 생성
        PathfindHallways();

    }

    private void PlaceRooms()
    {
        for(int i =0; i<roomCount;i++)
        {
            Vector2Int location = new Vector2Int(random.Next(0, size.x), random.Next(0, size.y));
            Vector2Int roomSize = new Vector2Int(random.Next(1, roomMaxSize.x + 1), random.Next(1, roomMaxSize.y + 1));

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

            foreach(Room room in rooms)
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
                if(newRoom.bounds.size.x > 2 && newRoom.bounds.size.y > 2)
                {
                    Debug.Log("방이 추가됨");
                    rooms.Add(newRoom);
                    PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);

                    foreach (var pos in newRoom.bounds.allPositionsWithin)
                    {
                        grid[pos] = CellType.Room;
                    }
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
            Debug.DrawLine(checkEdgePosition_U, checkEdgePosition_V, Color.blue, Mathf.Infinity);

            if (random.NextDouble() < 0.125) // 버린다는 건가? 특정 엣지들을 고르는 건 알겠다.
                                             // 문제는 왜 고르는거지? 모든 통로를 만드는건 비효율적이야
                                             // 근데 랜덤한 식으로 통로를 구하면 어떤 방은 못가지 않나?
            {
                selectedEdges.Add(edge);

                //if문안에서 실행되는거니까 저 범위안에 있는 녀석들만 들어와서 실행되겠지.
                Debug.DrawLine(checkEdgePosition_U, checkEdgePosition_V, Color.green, Mathf.Infinity);
                //Debug.Log("SelectedEdges : " + edge.ToString());
                Debug.DrawRay(checkEdgePosition_U, Vector3.up * 13f, Color.black, Mathf.Infinity);
                Debug.DrawRay(checkEdgePosition_V, Vector3.up * 15f, Color.white, Mathf.Infinity);
                
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
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center; //임시로 부동소수점(float)으로 중앙값 받아옴
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            var path = aStar.FindPath(startPos, endPos, (Pathfinder2D.Node a, Pathfinder2D.Node b) =>
            {
                var pathCost = new Pathfinder2D.PathCost();

                pathCost.cost = Vector2Int.Distance(b.Position, endPos); // heuristic

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

                    if (grid[current] == CellType.None)
                    {
                        grid[current] = CellType.Hallway; // 빈방이면 복도로 바꾼다.
                    }

                    if (i > 0)
                    {
                        var prev = path[i - 1];

                        var delta = current - prev;
                    }

                }

                foreach (var pos in path)
                {
                    if (grid[pos] == CellType.Hallway)
                    {
                        PlaceHallway(pos);
                    }
                }


            }
        }
    }


    private void PlaceCube(Vector2Int location, Vector2Int size, Material material)
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

        if(size.x > 2 && size.y > 2)
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
                    Vector3 createPos = new Vector3(location.x, 0.5f, location.y) + new Vector3(j, 0, i);

                    //코너 i=0,j=0 , i=0,j=size.x-1 , i=size.y-1,j=0 , i=size.y-1 j=size.x
                    if (i == 0 && j == 0)
                    {

                        GameObject cornerBL = Instantiate(cornerPrefab, createPos, Quaternion.identity);
                        cornerBL.GetComponent<Transform>().rotation = Quaternion.Euler(0, -180, 0);
                        cornerBL.gameObject.name = "Prefab_" + count + "cornerBL";
                        continue;
                    }
                    else if(i==0 && j == size.x-1)
                    {
                        GameObject cornerBR = Instantiate(cornerPrefab, createPos, Quaternion.identity);
                        cornerBR.GetComponent<Transform>().rotation = Quaternion.Euler(0, -270, 0);
                        cornerBR.gameObject.name = "Prefab_" + count + "cornerBR";
                        continue;
                    }
                    else if (i == size.y -1 && j == 0)
                    {
                        GameObject cornerUL = Instantiate(cornerPrefab, createPos, Quaternion.identity);
                        cornerUL.GetComponent<Transform>().rotation = Quaternion.Euler(0, -90, 0);
                        cornerUL.gameObject.name = "Prefab_" + count + "cornerUL";
                        continue;
                    }
                    else if (i == size.y-1 && j == size.x - 1)
                    {
                        GameObject cornerUR = Instantiate(cornerPrefab, createPos, Quaternion.identity);
                        //cornerUR.GetComponent<Transform>().rotation = Quaternion.Euler(0, -180, 0);
                        cornerUR.gameObject.name = "Prefab_" + count + "cornerUR";
                        continue;
                    }
                    //벽
                    else if(i == 0 && (j != 0 || j != size.x-1))
                    {
                        GameObject wallBottom = Instantiate(wallPrefab, createPos, Quaternion.identity);
                        wallBottom.GetComponent<Transform>().rotation = Quaternion.Euler(0,90,0);
                        wallBottom.gameObject.name = "Prefab_" + count + "wallBottom";
                        continue;
                    }
                    else if(i == size.y -1 && (j != 0 || j != size.x - 1))
                    {
                        GameObject wallUp = Instantiate(wallPrefab, createPos, Quaternion.identity);
                        wallUp.GetComponent<Transform>().rotation = Quaternion.Euler(0, 270, 0);
                        wallUp.gameObject.name = "Prefab_" + count + "wallUp";
                        continue;
                    }
                    else if ((i != 0 || i != size.y - 1) && j == 0)
                    {
                        GameObject wallLeft = Instantiate(wallPrefab, createPos, Quaternion.identity);
                        wallLeft.GetComponent<Transform>().rotation = Quaternion.Euler(0, 180, 0);
                        wallLeft.gameObject.name = "Prefab_" + count + "wallLeft";
                        continue;
                    }
                    else if ((i != 0 || i != size.y - 1) && j == size.x - 1)
                    {
                        GameObject wallRight = Instantiate(wallPrefab, createPos, Quaternion.identity);
                        wallRight.gameObject.name = "Prefab_" + count + "wallRight";
                        continue;
                    }


                    GameObject go = Instantiate(cubePrefab, createPos, Quaternion.identity);
                    //go.gameObject.transform.localScale = new Vector3(1, 3, 1);
                    int index = size.x * i + j;
                    go.gameObject.name = "Prefab_" +count+" (" + index +")";
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

    private void PlacePassageCube(Vector2Int location, Vector2Int size, Material material)
    {
        GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0.5f, location.y), Quaternion.identity);
        go.GetComponent<Transform>().localScale = new Vector3(size.x, 1, size.y); // 방의 local scale 방 크기를 정할 수 있다.
        //go.GetComponent<MeshRenderer>().material = material;
    }

    private void PlaceRoom(Vector2Int location, Vector2Int size)
    {
        PlaceCube(location, size, redMaterial);
    }

    private void PlaceHallway(Vector2Int location)
    {
        PlacePassageCube(location, new Vector2Int(1, 1), blueMaterial);
    }

    private IEnumerator CreateHallway(Vector2Int location, Vector2Int size, Material material)
    {

        yield return new WaitForSeconds(0.3f);
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
