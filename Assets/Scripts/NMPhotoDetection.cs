using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using UnityEngine.Windows.WebCam;
using Microsoft.MixedReality.Toolkit;

public class NMPhotoDetection : MonoBehaviour
{
    // Initialize necessary variables for photo capture and processing
    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;
    public float threshold1 = 8000; // First threshold for edge detection
    public float threshold2 = 1000; // Second threshold for edge detection
    public float p = 5, l = 5; // Parameters to adjust the processing grid
    int n1, n2, m1, m2; // Variables to store grid positions
    CameraParameters cameraParameters = new CameraParameters(); // Camera parameters for capturing images
    Renderer rndr; // Renderer to display the processed image
    Texture texture = null; // Texture to hold the original image

    // Start is called before the first frame update
    void Start()
    {
        rndr = GetComponent<Renderer>(); // Get the Renderer component attached to this GameObject
        // Select the highest resolution supported by the camera
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height); // Create a new texture with the camera's resolution

        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;
            cameraParameters.hologramOpacity = 0.5f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
            print(cameraResolution.width); // Debug print the camera resolution width
            print(cameraResolution.height); // Debug print the camera resolution height

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                // Take a picture
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }

    private void Update()
    {
        // Continuously take pictures
        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }

    // Callback function to process the captured photo
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into our target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        // Duplicate the original texture and prepare for edge detection
        var cols = targetTexture.GetPixels32(0);
        float[] values = new float[cols.Length]; // Intensity values
        float[] Gx = new float[cols.Length]; // Gradient in x-direction
        float[] Gy = new float[cols.Length]; // Gradient in y-direction
        float[] G = new float[cols.Length]; // Gradient magnitude
        float[] GG = new float[cols.Length]; // Duplicate gradient magnitude for non-maximum suppression
        float[] teta = new float[cols.Length]; // Gradient direction

        // Calculate grid positions based on texture dimensions and parameters p and l
        n1 = targetTexture.height / 10 * ((Mathf.FloorToInt((10 - p) / 2))) + 1;
        n2 = targetTexture.height / 10 * (10 - (Mathf.FloorToInt((10 - p) / 2))) - 1;
        m1 = targetTexture.width / 10 * ((Mathf.FloorToInt((10 - l) / 2))) + 1;
        m2 = targetTexture.width / 10 * (10 - (Mathf.FloorToInt((10 - l) / 2))) - 1;

        // Convert color pixels to grayscale intensity values
        for (int i = 0; i < cols.Length; ++i)
        {
            values[i] = 0.587f * cols[i].g + 0.299f * cols[i].r + 0.114f * cols[i].b;
        }

        // Smooth the image using a median filter
        for (int i = n1; i < n2; i += 1)
        {
            for (int j = m1; j < m2; j += 1)
            {
                float[] val = new float[8] {
                    values[i * targetTexture.width + j - 1], values[i * targetTexture.width + j + 1],
                    values[i * targetTexture.width + j - targetTexture.width], values[i * targetTexture.width + j + targetTexture.width],
                    values[i * targetTexture.width + j - targetTexture.width + 1], values[i * targetTexture.width + j + targetTexture.width -1],
                    values[i * targetTexture.width + j - targetTexture.width - 1], values[i * targetTexture.width + j + targetTexture.width + 1] };
                Array.Sort(val); // Sort the neighborhood values
                values[i * targetTexture.width + j] = (val[3] + val[4]) / 2; // Set the pixel value to the median
            }
        }

        // Compute gradients Gx, Gy, and the magnitude G
        for (int i = n1; i < n2; i += 1)
        {
            for (int j = m1; j < m2; j += 1)
            {
                Gx[i * targetTexture.width + j] = -1 * values[i * targetTexture.width + j - targetTexture.width - 1] + values[i * targetTexture.width + j - targetTexture.width + 1] - 2 * values[i * targetTexture.width + j - 1] + 2 * values[i * targetTexture.width + j + 1] - 1 * values[i * targetTexture.width + j + targetTexture.width - 1] + values[i * targetTexture.width + j + targetTexture.width + 1];
                Gy[i * targetTexture.width + j] = -1 * values[i * targetTexture.width + j - targetTexture.width - 1] - 1 * values[i * targetTexture.width + j - targetTexture.width + 1] - 2 * values[i * targetTexture.width + j - targetTexture.width] + 2 * values[i * targetTexture.width + j + targetTexture.width] + 1 * values[i * targetTexture.width + j + targetTexture.width - 1] + values[i * targetTexture.width + j + targetTexture.width + 1];
                G[i * targetTexture.width + j] = Mathf.Pow(Gx[i * targetTexture.width + j], 2) + Mathf.Pow(Gy[i * targetTexture.width + j], 2);
                GG[i * targetTexture.width + j] = Mathf.Pow(Gx[i * targetTexture.width + j], 2) + Mathf.Pow(Gy[i * targetTexture.width + j], 2);
                teta[i * targetTexture.width + j] = (Mathf.Atan(Gy[i * targetTexture.width + j] / Gx[i * targetTexture.width + j]));
            }
        }

        // Apply non-maximum suppression to thin out the edges
        for (int i = n1; i < n2; i += 1)
        {
            for (int j = m1; j < m2; j += 1)
            {
                var aa = 0.0f;
                var bb = 0.0f;
                // Determine the direction of the edge
                if (teta[i * targetTexture.width + j] > 1.178f | teta[i * targetTexture.width + j] < -1.178f)
                {
                    aa = GG[i * targetTexture.width + j - targetTexture.width]; bb = GG[i * targetTexture.width + j + targetTexture.width];
                }
                else if (teta[i * targetTexture.width + j] > 0.393f & teta[i * targetTexture.width + j] < 1.178f)
                {
                    aa = GG[i * targetTexture.width + j - targetTexture.width - 1]; bb = GG[i * targetTexture.width + j + targetTexture.width + 1];
                }
                else if ((teta[i * targetTexture.width + j] > -1.178f & teta[i * targetTexture.width + j] < -0.393f))
                {
                    aa = GG[i * targetTexture.width + j - targetTexture.width + 1]; bb = GG[i * targetTexture.width + j + targetTexture.width - 1];
                }
                else
                {
                    aa = GG[i * targetTexture.width + j - 1]; bb = GG[i * targetTexture.width + j + 1];
                }

                // Suppress pixels that are not part of an edge
                if (GG[i * targetTexture.width + j] < aa | GG[i * targetTexture.width + j] < bb)
                {
                    G[i * targetTexture.width + j] = 0;
                }
            }
        }

        // Apply double threshold and finalize the edge detection
        for (int i = n1; i < n2; i += 1)
        {
            cols[i * targetTexture.width + m1] = Color.blue;
            cols[i * targetTexture.width + m2] = Color.blue;
            for (int j = m1; j < m2; j += 1)
            {
                cols[n1 * targetTexture.width + j] = Color.blue;
                cols[n2 * targetTexture.width + j] = Color.blue;
                // Apply the first threshold
                if (G[i * targetTexture.width + j] > threshold1)
                {
                    cols[i * targetTexture.width + j] = Color.red;
                }
                // Apply the second threshold
                else if (G[i * targetTexture.width + j] > threshold2 & G[i * targetTexture.width + j - 1] > threshold1 & G[i * targetTexture.width + j + 1] > threshold1)
                {
                    cols[i * targetTexture.width + j] = Color.red;
                }
                else if (G[i * targetTexture.width + j] > threshold2 & G[i * targetTexture.width + j - targetTexture.width] > threshold1 & G[i * targetTexture.width + j + targetTexture.width] > threshold1)
                {
                    cols[i * targetTexture.width + j] = Color.red;
                }
                else if (G[i * targetTexture.width + j] > threshold2 & G[i * (targetTexture.width + 1) + j - 1] > threshold1 & G[i * (targetTexture.width - 1) + j + 1] > threshold1)
                {
                    cols[i * targetTexture.width + j] = Color.red;
                }
                else if (G[i * targetTexture.width + j] > threshold2 & G[i * (targetTexture.width - 1) + j - 1] > threshold1 & G[i * (targetTexture.width + 1) + j + 1] > threshold1)
                {
                    cols[i * targetTexture.width + j] = Color.red;
                }
            }
        }
        targetTexture.SetPixels32(cols); // Update the texture with processed colors
        targetTexture.Apply(); // Apply the changes to the texture
        rndr.material.mainTexture = targetTexture; // Set the processed texture as the material's main texture
    }
}
