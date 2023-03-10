using static LumaDX.Objects;

namespace LumaDX;

/// <summary>
/// Collection of standard meshes which have already been created
/// </summary>
public static class PresetMesh
{

    public static readonly Mesh Cube = new Mesh
    (
        vertices: new float[]
        {
            // back face
            -1, -1, -1,
             1,  1, -1,
             1, -1, -1,
             
             1,  1, -1, 
            -1, -1, -1,
            -1,  1, -1, 

            // front face
            -1, -1,  1, 
             1, -1,  1,
             1,  1,  1, 

             1,  1,  1, 
            -1,  1,  1,
            -1, -1,  1, 

            // left face
            -1,  1,  1, 
            -1,  1, -1,
            -1, -1, -1,

            -1, -1, -1, 
            -1, -1,  1,
            -1,  1,  1, 
            
            // right face
             1,  1,  1, 
             1, -1, -1,
             1,  1, -1, 

             1, -1, -1, 
             1,  1,  1,
             1, -1,  1, 

            // bottom face
            -1, -1, -1, 
             1, -1, -1,
             1, -1,  1, 

             1, -1,  1, 
            -1, -1,  1,
            -1, -1, -1, 

            // top face
            -1,  1, -1, 
             1,  1,  1,
             1,  1, -1,

             1,  1,  1, 
            -1,  1, -1,
            -1,  1,  1, 
             
        },

        texCoords: new float[]
        {
            //back face
            0,0, 1,1, 1,0,
            1,1, 0,0, 0,1,
                
            // front face
            0,0, 1,0, 1,1,
            1,1, 0,1, 0,0,
                
            // left face
            1,0, 1,1, 0,1,
            0,1, 0,0, 1,0,
                
            // right face
            1,0, 0,1, 1,1,
            0,1, 1,0, 0,0,
                
            // bottom face
            0,1, 1,1, 1,0,
            1,0, 0,0, 0,1,
                
            // top face
            0,1, 1,0, 1,1,
            1,0, 0,1, 0,0,
        },
            
        normals: new float[]
        {
            //back face
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,

            // front face
            0,0,1,
            0,0,1,
            0,0,1,
            0,0,1,
            0,0,1,
            0,0,1,
                
            // left face
            -1,0,0,
            -1,0,0,
            -1,0,0,
            -1,0,0,
            -1,0,0,
            -1,0,0,
                
            // right face
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
                
            // bottom face
            0,-1,0,
            0,-1,0,
            0,-1,0,
            0,-1,0,
            0,-1,0,
            0,-1,0,
                
            // top face
            0,1,0,
            0,1,0,
            0,1,0,
            0,1,0,
            0,1,0,
            0,1,0,
        },
            
        tangents: new float[]
        {
            //back face
            -1,0,0,
            -1,0,0,
            -1,0,0,
            -1,0,0,
            -1,0,0,
            -1,0,0,

            // front face
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
                
            // left face
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,
                
            // right face
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,
            0,0,-1,
                
            // bottom face
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
                
            // top face
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
            1,0,0,
        }

    );
    
    
    
    public static readonly Mesh Triangle = new Mesh
    (
        vertices: new float[]
        {
            -1, -1, 0,
            1, -1, 0, 
            0,  1, 0,
            
            
        },

        texCoords: new float[]
        {
            0,0, 1,0, 0.5f,1,
        },
            
        normals: new float[]
        {
            0,0,-1,
            0,0,-1,
            0,0,-1,
        }

    );    
    
    public static readonly Mesh Square = new Mesh
    (
        vertices: new float[]
        {
            -1, -1, 0,
             1, -1, 0, 
            -1,  1, 0,

            1, -1, 0, 
             1,  1, 0,
            -1,  1, 0,
        },

        texCoords: new float[]
        {
            0,0, 1,0, 0,1,
            1,0, 1,1, 0,1,
        },
            
        normals: new float[]
        {
            0,0,1,
            0,0,1,
            0,0,1,
            0,0,1,
            0,0,1,
            0,0,1,
        },
        
        tangents: new float[]
        {
            1, 0, 0,
            1, 0, 0,
            1, 0, 0,
            1, 0, 0,
            1, 0, 0,
            1, 0, 0,
        }

    );

    public static readonly Mesh Icosahedron = new Mesh(
        vertices: new float[]
        {
            -0.5257311f, 0.8506508f, 0.0f,
            0.5257311f, 0.8506508f, 0.0f,
            -0.5257311f, -0.8506508f, 0.0f,
            0.5257311f, -0.8506508f, 0.0f,
            0.0f, -0.5257311f, 0.8506508f,
            0.0f, 0.5257311f, 0.8506508f,
            0.0f, -0.5257311f, -0.8506508f,
            0.0f, 0.5257311f, -0.8506508f,
            0.8506508f, 0.0f, -0.5257311f,
            0.8506508f, 0.0f, 0.5257311f,
            -0.8506508f, 0.0f, -0.5257311f,
            -0.8506508f, 0.0f, 0.5257311f
        },
        
        indices: new int[]
        {
            0, 11, 5,
            0, 5, 1,
            0, 1, 7,
            0, 7, 10,
            0, 10, 11,
            1, 5, 9,
            5, 11, 4,
            11, 10, 2,
            10, 7, 6,
            7, 1, 8,
            3, 9, 4,
            3, 4, 2,
            3, 2, 6,
            3, 6, 8,
            3, 8, 9,
            4, 9, 5,
            2, 4, 11,
            6, 2, 10,
            8, 6, 7,
            9, 8, 1
        }
    );
}


public static class PresetMaterial
{
        
    // http://devernay.free.fr/cours/opengl/materials.html
    
    
    public static readonly Material Default = new Material(
        0.5f,0.5f,0.5f,
        1f,1f,1f,
        1f,1f,1f,
        64f
    );
        
    public static readonly Material Emerald = new Material(
        0.0215f,0.1745f,0.0215f,
        0.07568f,0.61424f,0.07568f,
        0.633f,0.727811f,0.633f,
        76.8f
    );

    public static readonly Material Jade = new Material(
        0.135f,0.2225f,0.1575f,
        0.54f,0.89f,0.63f,
        0.316228f,0.316228f,0.316228f,
        12.8f
    );
        
    public static readonly Material Obsidian = new Material(
        0.05375f,0.05f,0.06625f,
        0.18275f,0.17f,0.22525f,
        0.332741f,0.328634f,0.346435f,
        48.4f
    );

    public static readonly Material Pearl = new Material(
        0.25f, 0.20725f, 0.20725f,
        1f, 0.829f, 0.829f,
        0.296648f, 0.296648f, 0.296648f,
        11.264f
    );
        
    public static readonly Material Ruby = new Material(
        0.1745f,0.01175f,0.01175f,
        0.61424f,0.04136f,0.04136f,
        0.727811f,0.626959f,0.626959f,
        76.8f
    );
    
    public static readonly Material Turquoise = new Material(
        0.1f,0.18725f,0.1745f,
        0.396f,0.74151f,0.69102f,
        0.297254f,0.30829f,0.306678f,
        12.8f
    );
    
    public static readonly Material Brass = new Material(
        0.329412f,0.223529f,0.027451f,
        0.780392f,0.568627f,0.113725f,
        0.992157f,0.941176f,0.807843f,
        27.89743616f
    );
    
    public static readonly Material Bronze = new Material(
        0.2125f,0.1275f,0.054f,
        0.714f,0.4284f,0.18144f,
        0.393548f,0.271906f,0.166721f,
        25.6f
    );
    
    public static readonly Material Chrome = new Material(
        0.25f,0.25f,0.25f,
        0.4f,0.4f,0.4f,
        0.774597f,0.774597f,0.774597f,
        76.8f
    );
    
    public static readonly Material Copper = new Material(
        0.19125f,0.0735f,0.0225f,
        0.7038f,0.27048f,0.0828f,
        0.256777f,0.137622f,0.086014f,
        12.8f
    );
    
    public static readonly Material Gold = new Material(
        0.24725f,0.1995f,0.0745f,
        0.75164f,0.60648f,0.22648f,
        0.628281f,0.555802f,0.366065f,
        51.2f
    );
    
    public static readonly Material Silver = new Material(
        0.19225f,0.19225f,0.19225f,
        0.50754f,0.50754f,0.50754f,
        0.508273f,0.508273f,0.508273f,
        51.2f
    );
    
    public static readonly Material BlackPlastic = new Material(
        0.0f,0.0f,0.0f,
        0.01f,0.01f,0.01f,
        0.50f,0.50f,0.50f,
        32f
    );
    
    public static readonly Material CyanPlastic = new Material(
        0.0f,0.1f,0.06f,
        0.0f,0.50980392f,0.50980392f,
        0.50196078f,0.50196078f,0.50196078f,
        32f
    );
    
    public static readonly Material GreenPlastic = new Material(
        0.0f,0.0f,0.0f,
        0.1f,0.35f,0.1f,
        0.45f,0.55f,0.45f,
        32f
    );
    
    public static readonly Material RedPlastic = new Material(
        0.0f,0.0f,0.0f,
        0.5f,0.0f,0.0f,
        0.7f,0.6f,0.6f,
        32f
    );
    
    public static readonly Material WhitePlastic = new Material(
        0.0f,0.0f,0.0f,
        0.55f,0.55f,0.55f,
        0.70f,0.70f,0.70f,
        32f
    );
    
    public static readonly Material YellowPlastic = new Material(
        0.0f,0.0f,0.0f,
        0.5f,0.5f,0.0f,
        0.60f,0.60f,0.50f,
        32f
    );
    
    public static readonly Material BlackRubber = new Material(
        0.02f,0.02f,0.02f,
        0.01f,0.01f,0.01f,
        0.4f,0.4f,0.4f,
        10f
    );
    
    public static readonly Material CyanRubber = new Material(
        0.0f,0.05f,0.05f,
        0.4f,0.5f,0.5f,
        0.04f,0.7f,0.7f,
        10f
    );
    
    public static readonly Material GreenRubber = new Material(
        0.0f,0.05f,0.0f,
        0.4f,0.5f,0.4f,
        0.04f,0.7f,0.04f,
        10f
    );
    
    public static readonly Material RedRubber = new Material(
        0.05f,0.0f,0.0f,
        0.5f,0.4f,0.4f,
        0.7f,0.04f,0.04f,
        10f
    );
    
    public static readonly Material WhiteRubber = new Material(
        0.05f,0.05f,0.05f,
        0.5f,0.5f,0.5f,
        0.7f,0.7f,0.7f,
        10f
    );
    
    public static readonly Material YellowRubber = new Material(
        0.05f,0.05f,0.0f,
        0.5f,0.5f,0.4f,
        0.7f,0.7f,0.04f,
        10f
    );
    
}