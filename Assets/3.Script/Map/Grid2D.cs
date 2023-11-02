using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid2D<T>
{
    T[] data;

    public Vector2Int Size { get; private set; }
    public Vector2Int Offset { get; set; }

    public Grid2D(Vector2Int size, Vector2Int offset)
    {
        Size = size;
        Offset = offset;

        data = new T[size.x * size.y];
    }

    //반환값이 int 1차원 배열이다. 하나의 값으로 반환함으로써 Room의 Index를 가져올 수 있다.
    public int GetIndex(Vector2Int pos)
    {
        return pos.x + (Size.x * pos.y);
    }

    public bool InBounds(Vector2Int pos)
    {
        // 0,0위치에서 30x30 넓이의 바탕그리드에서 받아온 매개변수의 위치가 그 안에 속해있다면 바운드 안에 있다.
        return new RectInt(Vector2Int.zero, Size).Contains(pos + Offset); 
    }

    public T this[int x, int y]
    {
        get
        {
            return this[new Vector2Int(x, y)];
        }
        set
        {
            this[new Vector2Int(x, y)] = value;
        }
    }

    public T this[Vector2Int pos]
    {
        get
        {
            pos += Offset;
            return data[GetIndex(pos)];
        }
        set
        {
            pos += Offset;
            data[GetIndex(pos)] = value;
        }
    }


}
