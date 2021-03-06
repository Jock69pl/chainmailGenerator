﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class helps to generate heightMap and normalMap
// It is raytracing input 3D model to planar textures
public class StampGenerator {
    // resolutian of both output textures
    public int resolution = 512;

    // item is 3D object that will be raytraced to textures
    private GameObject item;

    // generated textures
    private Texture2D textureHeight;
    private Texture2D normalTexture;
    private Texture2D normalTextureFromHeight;
    private Texture2D edgeTexture;
    // generated 2D array of raytraced height
    // we don't using generated heightMap because
    // in float we can have more accurate data
    private float[,] heights;
    private bool[,] presence;
    private float strengthOfGeneratedNormalMap = -1f;
    private float strengthOfGeneratedEdgeMap = -1f;
    private Vector3 rotation;

    public StampGenerator(GameObject item, int resolution, Vector3 rotation) {
        this.item = item;
        if (resolution > 0)
            this.resolution = resolution;
        this.rotation = rotation;
    }

    // returns generated heightMap of input 3D object
    public Texture2D getHeightMap() {
        // if texture was already generated there is no need
        // to generate it again
        if (textureHeight != null)
            return textureHeight;

        MeshCollider collider = item.GetComponent<MeshCollider>();
        GameObject go = null;
        if (!collider) {
            //Add a collider to our source object if it does not exist.
            go = GameObject.Instantiate(item, new Vector3(), Quaternion.Euler(rotation)) as GameObject;
            collider = go.AddComponent<MeshCollider>();
        }
        Bounds bounds = collider.bounds;
        textureHeight = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);

        // Do raycasting samples over the object to see what terrain heights should be
        heights = new float[resolution, resolution];
        presence = new bool[resolution, resolution];
        Ray ray = new Ray(new Vector3(bounds.min.x, bounds.max.y + bounds.size.y, bounds.min.z), -Vector3.up);
        RaycastHit hit = new RaycastHit();
        float meshHeightInverse = 1 / bounds.size.y;
        Vector3 rayOrigin = ray.origin;

        int maxHeight = heights.GetLength(0);
        int maxLength = heights.GetLength(1);

        // there is frame because we wanna to have empty line outside object
        int frame = 2;
        int maxHeightScan = maxHeight - frame * 2;
        int maxLengthScan = maxLength - frame * 2;

        // biggest and smallest raycasted value
        float top = 0, bottom = 1;

        float height = 0.0f;
        Color blank = new Color(0, 0, 0, 0);

        Vector2 stepXZ = new Vector2(bounds.size.x / maxLengthScan, bounds.size.z / maxHeightScan);

        for (int zCount = 0; zCount < maxHeightScan; zCount++) {
            for (int xCount = 0; xCount < maxLengthScan; xCount++) {

                height = 0.0f;

                if (collider.Raycast(ray, out hit, bounds.size.y * 3)) {
                    height = (hit.point.y - bounds.min.y) * meshHeightInverse;
                }
                //clamp
                if (height <= 0)
                    height = 0;
                else {
                    presence[zCount + frame, xCount + frame] = true;
                    if (height < bottom)
                        bottom = height;
                    if (height > top)
                        top = height;
                }

                heights[zCount + frame, xCount + frame] = height;
                rayOrigin.x += stepXZ[0];
                ray.origin = rayOrigin;
            }

            rayOrigin.z += stepXZ[1];
            rayOrigin.x = bounds.min.x;
            ray.origin = rayOrigin;
        }

        float mult = 1f / (top - bottom);

        for (int zCount = 0; zCount < maxHeight; zCount++) {
            for (int xCount = 0; xCount < maxLength; xCount++) {
                height = heights[zCount, xCount];

                //clamp negative value as black color
                if (presence[zCount, xCount]) {
                    height = (heights[zCount, xCount] - bottom) * mult;
                    textureHeight.SetPixel(zCount, xCount, new Color(height, height, height, 1));
                } else {
                    textureHeight.SetPixel(zCount, xCount, blank);
                }
            }
        }
        textureHeight.wrapMode = TextureWrapMode.Clamp;

        // Actually apply all 'setPixel' changes
        textureHeight.Apply();

        // objct was created only for raycasting, we don't need it now
        if (go != null)
            GameObject.Destroy(go);

        return textureHeight;
    }

    // returns generated heightMap of input 3D object
    public Texture2D getNormalMap() {
        // if texture was already generated there is no need
        // to generate it again
        if (normalTexture != null)
            return normalTexture;

        MeshCollider collider = item.GetComponent<MeshCollider>();
        GameObject go = null;
        if (!collider) {
            //Add a collider to our source object if it does not exist.
            go = GameObject.Instantiate(item, new Vector3(), Quaternion.Euler(rotation)) as GameObject;
            collider = go.AddComponent<MeshCollider>();
        }
        Bounds bounds = collider.bounds;
        normalTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);

        // Do raycasting samples over the object to see what terrain heights should be
        Ray ray = new Ray(new Vector3(bounds.min.x, bounds.max.y + bounds.size.y, bounds.min.z), -Vector3.up);
        RaycastHit hit = new RaycastHit();
        Vector3 rayOrigin = ray.origin;
        Color blank = new Color(0, 0, 0, 0);

        // there is frame because we wanna to have empty line outside object
        int frame = 2;
        int maxHeightScan = resolution - frame * 2;
        int maxLengthScan = resolution - frame * 2;

        Vector2 stepXZ = new Vector2(bounds.size.x / maxLengthScan, bounds.size.z / maxHeightScan);

        // filling with blank color first
        for (int zCount = 0; zCount < resolution; zCount++) {
            for (int xCount = 0; xCount < resolution; xCount++) {
                normalTexture.SetPixel(zCount, xCount, blank);
            }
        }

        // scaning to normal vectors
        for (int zCount = 0; zCount < maxHeightScan; zCount++) {
            for (int xCount = 0; xCount < maxLengthScan; xCount++) {
                if (collider.Raycast(ray, out hit, bounds.size.y * 3)) {
                    normalTexture.SetPixel(zCount + frame, xCount + frame,
                        new Color(
                            -hit.normal.x / 2 + 0.5f,
                            -hit.normal.z / 2 + 0.5f,
                            hit.normal.y / 2 + 0.5f,
                            1));
                }

                rayOrigin.x += stepXZ[0];
                ray.origin = rayOrigin;
            }

            rayOrigin.z += stepXZ[1];
            rayOrigin.x = bounds.min.x;
            ray.origin = rayOrigin;
        }

        normalTexture.wrapMode = TextureWrapMode.Clamp;
        // Actually apply all 'setPixel' changes
        normalTexture.Apply();

        // objct was created only for raycasting, we don't need it now
        if (go != null)
            GameObject.Destroy(go);

        return normalTexture;
    }

    private Texture2D DTXnm2RGBA(Texture2D tex) {
        Color[] colors = tex.GetPixels();
        for (int i = 0; i < colors.Length; i++) {
            Color c = colors[i];
            c.r = c.a * 2 - 1;  //red<-alpha (x<-w)
            c.g = c.g * 2 - 1; //green is always the same (y)
            Vector2 xy = new Vector2(c.r, c.g); //this is the xy vector
            c.b = Mathf.Sqrt(1 - Mathf.Clamp01(Vector2.Dot(xy, xy))); //recalculate the blue channel (z)
            colors[i] = new Color(c.r * 0.5f + 0.5f, c.g * 0.5f + 0.5f, c.b * 0.5f + 0.5f); //back to 0-1 range
        }
        tex.SetPixels(colors); //apply pixels to the texture
        tex.Apply();
        return tex;
    }


    // returns generated normalMap of input 3D object
    // strength in argument is for how raised output texture should be
    public Texture2D getNormalMapFromHeight(float strength = 30) {
        // if texture was already generated there is no need
        // to generate it again
        // also strength must be the same
        if (normalTextureFromHeight != null && strengthOfGeneratedNormalMap == strength)
            return normalTextureFromHeight;
        else
            strengthOfGeneratedNormalMap = strength;

        // If heightMap wasn't generated, we should generate it now
        if (textureHeight == null)
            getHeightMap();

        normalTextureFromHeight = new Texture2D(textureHeight.width, textureHeight.height, TextureFormat.ARGB32, textureHeight.mipmapCount > 1);
        Color blank = new Color(0, 0, 0, 0);

        for (int y = 0; y < textureHeight.height; y++) {
            for (int x = 0; x < textureHeight.width; x++) {
                if (presence[x, y]) {
                    int x_1 = x - 1;
                    if (x_1 < 0)
                        x_1 = textureHeight.width - 1; // repeat the texture so use the opposit side
                    int x1 = x + 1;
                    if (x1 >= textureHeight.width)
                        x1 = 0; // repeat the texture so use the opposit side
                    int y_1 = y - 1;
                    if (y_1 < 0)
                        y_1 = textureHeight.height - 1; // repeat the texture so use the opposit side
                    int y1 = y + 1;
                    if (y1 >= textureHeight.height)
                        y1 = 0; // repeat the texture so use the opposit side
                    float grayX_1 = heights[x_1, y];
                    float grayX1 = heights[x1, y];
                    float grayY_1 = heights[x, y_1];
                    float grayY1 = heights[x, y1];
                    Vector3 vx = new Vector3(0, 1, (grayX_1 - grayX1) * strength);
                    Vector3 vy = new Vector3(1, 0, (grayY_1 - grayY1) * strength);
                    Vector3 n = Vector3.Cross(vy, vx).normalized;
                    Vector3 color = ((n + Vector3.one) * 0.5f);

                    normalTextureFromHeight.SetPixel(x, y, new Vector4(color.x, color.y, color.z, 1));
                } else {
                    normalTextureFromHeight.SetPixel(x, y, blank);
                }
            }
        }

        normalTextureFromHeight.wrapMode = TextureWrapMode.Clamp;
        normalTextureFromHeight.Apply();

        return normalTextureFromHeight;
    }

    public static void printNormalMap(Texture2D normalTexture, Texture2D textureHeight, int strength = 30) {
        for (int y = 0; y < textureHeight.height; y++) {
            for (int x = 0; x < textureHeight.width; x++) {
                int x_1 = x - 1;
                if (x_1 < 0)
                    x_1 = textureHeight.width - 1; // repeat the texture so use the opposit side
                int x1 = x + 1;
                if (x1 >= textureHeight.width)
                    x1 = 0; // repeat the texture so use the opposit side
                int y_1 = y - 1;
                if (y_1 < 0)
                    y_1 = textureHeight.height - 1; // repeat the texture so use the opposit side
                int y1 = y + 1;
                if (y1 >= textureHeight.height)
                    y1 = 0; // repeat the texture so use the opposit side
                float grayX_1 = textureHeight.GetPixel(x_1, y).r;
                float grayX1 = textureHeight.GetPixel(x1, y).r;
                float grayY_1 = textureHeight.GetPixel(x, y_1).r;
                float grayY1 = textureHeight.GetPixel(x, y1).r;
                Vector3 vx = new Vector3(0, 1, (grayX_1 - grayX1) * strength);
                Vector3 vy = new Vector3(1, 0, (grayY_1 - grayY1) * strength);
                Vector3 n = Vector3.Cross(vy, vx).normalized;
                Vector3 color = ((n + Vector3.one) * 0.5f);

                normalTexture.SetPixel(x, y, new Vector4(color.x, color.y, color.z, 1));
            }
        }

        normalTexture.wrapMode = TextureWrapMode.Clamp;
        normalTexture.Apply();
    }

    // returns generated normalMap of input 3D object
    // strength in argument is for how raised output texture should be
    public Texture2D getEdgeMap(Color color, float strength = 30, int spray = 2) {
        // if texture was already generated there is no need
        // to generate it again
        // also strength must be the same
        if (edgeTexture != null && strengthOfGeneratedEdgeMap == strength)
            return edgeTexture;
        else
            strengthOfGeneratedEdgeMap = strength;

        // If heightMap wasn't generated, we should generate it now
        if (textureHeight == null)
            getHeightMap();

        edgeTexture = new Texture2D(textureHeight.width, textureHeight.height, TextureFormat.ARGB32, textureHeight.mipmapCount > 1);
        Color blank = new Color(0, 0, 0, 0);

        for (int y = 0; y < textureHeight.height; y++) {
            for (int x = 0; x < textureHeight.width; x++) {
                if (presence[x, y]) {
                    float change = (1 - (getVariationX(x - spray, x + spray, y) + getVariationY(y - spray, y + spray, x)) / 2f * strength);

                    edgeTexture.SetPixel(x, y, new Color(color.r * change, color.g * change, color.b * change, 1));
                } else { edgeTexture.SetPixel(x, y, blank); }
            }
        }

        edgeTexture.wrapMode = TextureWrapMode.Clamp;
        edgeTexture.Apply();

        return edgeTexture;
    }

    private float getVariationX(int fromX, int toX, int y) {
        if (fromX < 0) { fromX = 0; }
        if (toX >= textureHeight.width) toX = textureHeight.width - 1;

        float min = heights[fromX++, y];
        float max = min;
        float tmp;

        while (fromX <= toX) {
            tmp = heights[fromX++, y];
            if (tmp > max) { max = tmp; } else if (tmp < min) { min = tmp; }
        }

        return Mathf.Abs(max - min);
    }

    private float getVariationY(int fromY, int toY, int x) {
        if (fromY < 0) { fromY = 0; }
        if (toY >= textureHeight.height) toY = textureHeight.height - 1;

        float min = heights[x, fromY++];
        float max = min;
        float tmp;

        while (fromY <= toY) {
            tmp = heights[x, fromY++];
            if (tmp > max) { max = tmp; } else if (tmp < min) { min = tmp; }
        }

        return Mathf.Abs(max - min);
    }
}