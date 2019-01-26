﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeighbourCreator {
    private Mesh mesh3d;
    private int current;
    private int neighbourRadius = 1;

    // list of unique verts around neighbours, values there can change after update
    // we need to recreatethat list using triVert array
    private List<Vector3> unique = new List<Vector3>();
    // list of shortcuts to verticles from 3D mesh, that only indexes so it shouldn't change
    // we cannot use just verticle list because they are doubled on edges because of uv and normals
    private List<int> verticles = new List<int>();
    // list of triangles. Verticles should be accessed from mesh3d.vertices
    // with mapping from triVert array
    private List<int> triangles = new List<int>();
    private List<Edge> edges = new List<Edge>();
    private List<int> index = new List<int>();
    private List<int> previousNeighborhoodIndexes = new List<int>();
    private List<Triangle3D> previousNeighborhood = new List<Triangle3D>();
    private List<Triangle3D> currentNeighborhood = new List<Triangle3D>();
    private bool[,] edgeConnections;
    private Vector3 crossMain;

    public NeighbourCreator(Mesh mesh3d, int current, int neighbourRadius) {
        this.mesh3d = mesh3d;
        this.current = current;
        this.neighbourRadius = neighbourRadius;

        edgeConnections = new bool[mesh3d.triangles.Length, mesh3d.triangles.Length];
        crossMain = getCross(mesh3d, current);
    }

    public Neighbour create() {
        fillFirstTriangle();
        fillRestOfTriangles();
        return new Neighbour(index, triangles, verticles, edges);
    }

    private Triangle3D getTriangleFor(int i) {
        return new Triangle3D(
                    mesh3d.vertices[mesh3d.triangles[i + 0]],
                    mesh3d.vertices[mesh3d.triangles[i + 1]],
                    mesh3d.vertices[mesh3d.triangles[i + 2]]);
    }

    // filling first triangle
    private void fillFirstTriangle() {
        // current 3D triangle
        Triangle3D p = getTriangleFor(current);
        unique.Add(p.p1);
        unique.Add(p.p2);
        unique.Add(p.p3);
        verticles.Add(mesh3d.triangles[current + 0]);
        verticles.Add(mesh3d.triangles[current + 1]);
        verticles.Add(mesh3d.triangles[current + 2]);
        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);
        index.Add(current + 0);
        index.Add(current + 1);
        index.Add(current + 2);
        previousNeighborhood.Add(p);
        previousNeighborhoodIndexes.Add(current);
    }

    private void fillRestOfTriangles() {
        for (int j = 0; j < neighbourRadius; j++) {
            // it's next iteration, we need to pass current neighbours as already used
            pushCurrentNeighborhoodAsPrevious();

            // iterating through every triangle
            for (int i = 0; i < mesh3d.triangles.Length; i += 3) {
                if (previousNeighborhoodIndexes.Contains(i)) {
                    // we used that triangle before as neighbour
                    continue;
                }

                Triangle3D n = getTriangleFor(i);

                if (isNeighbour(n, previousNeighborhood)) {
                    // n is neighbour to some triangle solved in previous iteration

                    currentNeighborhood.Add(n);
                    previousNeighborhoodIndexes.Add(i);

                    int[] indexOfNP = getIndexOfNP(n, i);
                    triangles.Add(indexOfNP[0]);
                    triangles.Add(indexOfNP[1]);
                    triangles.Add(indexOfNP[2]);
                    index.Add(i + 0);
                    index.Add(i + 1);
                    index.Add(i + 2);

                    addEdges(edges, edgeConnections, indexOfNP, getStrength(i));
                }
            }
        }
    }

    private void pushCurrentNeighborhoodAsPrevious() {
        foreach (Triangle3D n in currentNeighborhood) {
            previousNeighborhood.Add(n);
        }
        currentNeighborhood.Clear();
    }

    // get index of n-triangle p-oints
    // generation of indexes for triangle
    // if some point was used before on other triangle
    // then we should reuse them, triangles should be glued to each other
    private int[] getIndexOfNP(Triangle3D n, int i) {
        // trying to find indexed if they are used in already solved triangles
        int[] indexOfNP =  {
                     unique.IndexOf(n.p1),
                     unique.IndexOf(n.p2),
                     unique.IndexOf(n.p3)};

        // if index == -1 then point was't used before
        // we need to generate index and add it to unique list 
        // also it's new verticle in mesh
        if (indexOfNP[0] == -1) {
            unique.Add(n.p1);
            verticles.Add(mesh3d.triangles[i + 0]);
            indexOfNP[0] = unique.Count - 1;
        }
        if (indexOfNP[1] == -1) {
            unique.Add(n.p2);
            verticles.Add(mesh3d.triangles[i + 1]);
            indexOfNP[1] = unique.Count - 1;
        }
        if (indexOfNP[2] == -1) {
            unique.Add(n.p3);
            verticles.Add(mesh3d.triangles[i + 2]);
            indexOfNP[2] = unique.Count - 1;
        }

        return indexOfNP;
    }

    private Vector3 getCross(Mesh mesh3d, int k) {
        return Vector3.Cross(
            mesh3d.vertices[mesh3d.triangles[k + 1]] - mesh3d.vertices[mesh3d.triangles[k + 0]],
            mesh3d.vertices[mesh3d.triangles[k + 2]] - mesh3d.vertices[mesh3d.triangles[k + 0]]);
    }

    private bool isNeighbour(Triangle3D n, List<Triangle3D> list) {
        foreach (Triangle3D p in list) {
            if (n.isNeighbour(p)) {
                return true;
            }
        }
        return false;
    }

    // returns variable [0..1] that is saying how much that triangle 
    // is pararell to triangle with current object
    private float getStrength(int i) {
        Vector3 cross = getCross(mesh3d, i);
        return 1 - Vector3.Angle(crossMain, cross) / 180;
    }

    // filling list of edges that will be used to normalization of triangles
    // list won't contain for example edges in main triangle, because we don't wanna to distort him
    private void addEdges(List<Edge> edges, bool[,] edgeConnections, int[] indexOfNP, float strength) {
        // we need to iterate through indexOfNP (3 elements)
        for (int k = 0; k < indexOfNP.Length; k++) {
            // index need to be less than 3 because indexes 0,1,2 are for mother triangle
            if (indexOfNP[k] >= 3) {
                // we need to iterate through indexOfNP again
                // but choose all point that are not current k
                for (int j = 0; j < indexOfNP.Length; j++) {
                    if (k != j) {
                        // checking if that edge already exist
                        if (!edgeConnections[indexOfNP[j], indexOfNP[k]]) {
                            // not existing, we need to create that edge from j to k
                            // and mark that edge as created
                            // notice that j->k is different than k->j
                            edges.Add(new Edge(indexOfNP[j], indexOfNP[k], strength));
                            edgeConnections[indexOfNP[j], indexOfNP[k]] = true;
                        }
                    }
                }
            }
        }
    }
}
