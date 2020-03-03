using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UIElements;

//[ExecuteInEditMode]
public class RoomsGenerator : MonoBehaviour
{
    public GameObject roomBox;
    public List<GameObject> objects;
    public Camera mainCamera;
    public int splits = 4;
    public float maxRoomDepth = 4;

    public float gridSize;
    public int cols, rows;

    private List<GameObject> rooms;

    void Start()
    {
        rooms = new List<GameObject>();
        GenerateRooms();
    }

    void GenerateRooms()
    {

        float fovRad = mainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
        float camDist = Math.Abs(mainCamera.transform.position.z);
        
        float sh = camDist * Mathf.Tan(fovRad) * 2.0f;
        float sw = mainCamera.aspect * sh;

        gridSize = sw / splits;

        cols = splits;
        rows = (int)(sh / gridSize);

        float totW = cols * gridSize;
        float totH = rows * gridSize;

        mainCamera.farClipPlane = camDist + gridSize * maxRoomDepth;

        GameObject room;
        int roomId = 0;
        float boundsOffset = 0.01f;

        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                room = Instantiate(roomBox);
                room.transform.parent = transform;
                room.layer = roomId;
                room.SetActive(true);
                Vector3 scale = new Vector3(gridSize, gridSize, gridSize) * 0.9f;
                Vector3 positon = new Vector3();

                scale.z *= UnityEngine.Random.Range(1.0f, 4.0f);

                positon.x = i * gridSize - totW * 0.5f + gridSize * 0.5f;
                positon.y = j * gridSize - totH * 0.5f + gridSize * 0.5f;
                positon.z = scale.z * 0.5f;
                room.transform.localScale = scale;
                room.transform.localPosition = positon;

                

                Material mat = room.GetComponent<Renderer>().material;
                mat.SetVector("minBounds", positon - scale * .5f - Vector3.one * boundsOffset);
                mat.SetVector("maxBounds", positon + scale * .5f + Vector3.one * boundsOffset);

                roomId++;
            }
        }

    }

}