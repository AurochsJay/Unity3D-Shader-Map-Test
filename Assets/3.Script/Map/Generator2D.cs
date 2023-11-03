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
            //a.bounds.position.x : a���� ���ʰ��, b.bounds.position.x + b.bounds.size.x : b ���� �����ʰ��.
            //a���� ���ʰ�谡 b���� ������ ��躸�� ũ�ų� ���ٸ� �� ���� ��ġ�� �ʴ´�.
        }
        /* RectInt�� position ���� -> minx�� miny�� ��´�.
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


    //�� ��ȣ
    private int count = 1;
    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        random = new Random(); // Random�� �õ尪�� �ִ� ������ ���� ��� �ٲ�� ���� ��������
        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        //�����
        PlaceRooms();
        //��γ� �ﰢ����
        Triangulate();
        //��纹������
        CreateHallways();
        //����� ������ ����

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
                    Debug.Log("Intersect�� ���ͼ� add�� �ȵ�");
                    add = false;
                    break;
                }
            }

            if(newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y)
            {
                Debug.Log("�׸��� ���� ������ ���� ť�갡 �ִ���");
                add = false;
            }

            if(add)
            {
                if(newRoom.bounds.size.x > 2 && newRoom.bounds.size.y > 2)
                {
                    Debug.Log("���� �߰���");
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

        //Ȯ�ο뵵
        for (int i = 0; i < delaunay.Vertices.Count; i++)
        {
            Vector3 CheckPoint = new Vector3(delaunay.Vertices[i].Position.x, 0, delaunay.Vertices[i].Position.y);
            Debug.DrawRay(CheckPoint, Vector3.up * 8f, Color.red, Mathf.Infinity);
            //Debug.Log(delaunay.Vertices[i].Position);
        }

        Debug.Log("������ ���°ǰ�?" + delaunay.Edges.Count);
        for (int i = 0; i < delaunay.Edges.Count; i++)
        {
            //Debug.Log("����� ����" + i);
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

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U); //����Ʈ�� ������(0)

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges)
        {
            Vector3 checkEdgePosition_U = new Vector3(edge.U.Position.x - 0.3f, 0, edge.U.Position.y);
            Vector3 checkEdgePosition_V = new Vector3(edge.V.Position.x, 0, edge.V.Position.y);

            Debug.DrawRay(checkEdgePosition_U, Vector3.up * 5f, Color.blue, Mathf.Infinity);
            Debug.DrawRay(checkEdgePosition_V, Vector3.up * 5f, Color.green, Mathf.Infinity);
            //Debug.Log("remainingEdges�� �ִ� edge�� : " + edge.ToString());
            if (random.NextDouble() < 0.125)
            {
                selectedEdges.Add(edge);
                //Debug.Log("SelectedEdges : " + edge.ToString());
                //Debug.DrawRay(edge.U.Position, Vector3.up * 5f, Color.blue, Mathf.Infinity);
                //Debug.DrawRay(edge.V.Position, Vector3.up * 5f, Color.green, Mathf.Infinity);
            }
        }
    }


    private void PlaceCube(Vector2Int location, Vector2Int size, Material material)
    {
        //if(size.x <= 2 || size.y <=2) �̷������� ����� �� ���ϸ� �������� �ʰ� �� �� �ִ�.
        //������ ���� �ĳ���.

        //���ǿ� ���� ���� ���� ���Ѵ�.
        //�ڳ��̸�, Corner -> Rotate y�� �ְ�, BL(BottomLeft), BR(BottomRight), UL(UpLeft), UR(UpRight)
        //���Ա���, Door
        //���� ��躮�̸�, Wall

        //�ڳ��� ���� -> BL : location.x, BR : location.x + size.x, UL : location.y, UR : location.y + size.y
        //���Ա��� ���� -> ��ΰ� Ȯ���Ǿ����
        //���� �� -> �ڳʿ� ���Ա�, ���θ� ������ ��� ��

        if(size.x > 2 && size.y > 2)
        {
            //GameObject go = Instantiate(cubePrefab, new Vector3(location.x, 0, location.y), Quaternion.identity);
            //go.GetComponent<Transform>().localScale = new Vector3(size.x, 3, size.y); // ���� local scale �� ũ�⸦ ���� �� �ִ�.
            //go.GetComponent<MeshRenderer>().material = material;

            
            //��
            for(int i = 0; i < size.y; i++)
            {
                //��
                for (int j = 0; j < size.x; j++)
                {
                    Vector3 createPos = new Vector3(location.x, 0.5f, location.y) + new Vector3(j, 0, i);

                    //�ڳ� i=0,j=0 , i=0,j=size.x-1 , i=size.y-1,j=0 , i=size.y-1 j=size.x
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
                    //��
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
        
        
        //���� ������ size.x�� ���϶� size.y�� ���� ��
        //�׿� �ش��ϴ� prefab�� �����
        //���� ����ϰ� �ִ� �޼��带 PlaceRoom���� �ΰ� ��δ� PlaceHallway�� �Ἥ �ٸ��� ������ �ȴ�.
        //���� �������� �ǵ��� ���� ���� ������ ��ġ location���ٰ� �ʺ�� ���̸� ���ؼ� 
        //Ÿ�� �ϳ��ϳ� �����ϰ� �ٿ������ ���� ������?
        //for���� �Ἥ ù��° index�� ��� ���� �����(���� ���)
        //�ϳ��� ť�꿡 

    }

    private void PlaceRoom(Vector2Int location, Vector2Int size)
    {
        PlaceCube(location, size, redMaterial);
    }

    
}
