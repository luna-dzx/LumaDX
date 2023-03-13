using OpenTK.Mathematics;

namespace LumaDX;

public static class Objects
{
    /// <summary>
    /// Stores data for a standard VAO
    /// </summary>
    public class Mesh
    {
        public float[]? Vertices;
        public float[]? TexCoords;
        public float[]? Normals;
        public float[]? Tangents;

        public int[]? Indices;

        public int VertexBinding = 0;
        public int TexCoordBinding = 1;
        public int NormalBinding = 2;
        public int TangentBinding = 3;

        public Mesh(float[]? vertices = null, int[]? indices = null, float[]? texCoords = null, float[]? normals = null, float[]? tangents = null)
        {
            if (vertices?.Length == 0) vertices = null;
            if (texCoords?.Length == 0) texCoords = null;
            if (normals?.Length == 0) normals = null;
            if (tangents?.Length == 0) tangents = null;
            
            Vertices = vertices;
            Indices = indices;
            TexCoords = texCoords;
            Normals = normals;
            Tangents = tangents;
        }
        
        public Mesh FlipNormals()
        {
            for (int i = 0; i < Normals?.Length; i++)
            {
                Normals[i] *= -1;
            }

            return this;
        }

    }
    
    public class Material
    {
        public Vector3 Ambient;
        public Vector3 Diffuse;
        public Vector3 Specular;
        public float Shininess;
        public string Name = "Material";


        public Material(){}
            
        public Material(
            float ambientR, float ambientG, float ambientB,
            float diffuseR, float diffuseG, float diffuseB,
            float specularR, float specularG, float specularB,
            float shininess, string name = "Material"
        )
        {
            SetAmbient(ambientR, ambientG, ambientB);
            SetDiffuse(diffuseR, diffuseG, diffuseB);
            SetSpecular(specularR, specularG, specularB);
            SetShininess(shininess);
            Name = name;
        }

        public Material SetAmbient(Vector3 ambient) { Ambient = ambient; return this; }
        public Material SetAmbient(float r, float g, float b) { Ambient = new Vector3(r,g,b); return this; }
        public Material SetAmbient(float value) { Ambient = new Vector3(value,value,value); return this; }
        public Material SetDiffuse(Vector3 diffuse) { Diffuse = diffuse; return this; }
        public Material SetDiffuse(float r, float g, float b) { Diffuse = new Vector3(r,g,b); return this; }
        public Material SetDiffuse(float value) { Diffuse = new Vector3(value,value,value); return this; }
        public Material SetSpecular(Vector3 specular) { Specular = specular; return this; }
        public Material SetSpecular(float r, float g, float b) { Specular = new Vector3(r,g,b); return this; }
        public Material SetSpecular(float value) { Specular = new Vector3(value,value,value); return this; }
        public Material SetShininess(float shininess) { Shininess = shininess; return this; }
            
    }

    public class Light
    {
        public Vector3 Position;
        public Vector3 Ambient = Vector3.One;
        public Vector3 Diffuse = Vector3.One;
        public Vector3 Specular = Vector3.One;
        public Vector3 Attenuation = Vector3.UnitX;
        public Vector3 Direction = Vector3.UnitZ;
        private float cutOff = 1f;
        private float outerCutOff = 0f;
        
        public float GetCutOff() => cutOff;
        public float GetOuterCutOff() => outerCutOff;
        
        public Light PointMode(){cutOff = 1f; return this;}
        public Light SunMode(){cutOff = 0f; return this;}
        public Light SpotlightMode(float angle){cutOff = MathF.Cos(angle); outerCutOff = 0; return this;}
        public Light SpotlightMode(float angle, float outerAngle){cutOff = MathF.Cos(angle);outerCutOff = MathF.Cos(outerAngle); return this;}
        
        
        
        public Light SetPosition(Vector3 position) { Position = position; return this; }
        public Light SetPosition(float x, float y, float z) { Position = new Vector3(x,y,z); return this; }

        public Light UpdatePosition(ref ShaderProgram shaderProgram, string name)
        {
            shaderProgram.Uniform3(name + ".position", Position);
            return this;
        }

        public Light SetDirection(Vector3 direction) { Direction = direction; return this; }
        public Light SetDirection(float x, float y, float z) { Direction = new Vector3(x,y,z); return this; }

        public Light UpdateDirection(ref ShaderProgram shaderProgram, string name)
        {
            shaderProgram.Uniform3(name + ".direction", Direction);
            return this;
        }

        public Light SetAmbient(Vector3 ambient) { Ambient = ambient; return this; }
        public Light SetAmbient(float r, float g, float b) { Ambient = new Vector3(r,g,b); return this; }
        public Light SetAmbient(float value) { Ambient = new Vector3(value,value,value); return this; }
        public Light SetDiffuse(Vector3 diffuse) { Diffuse = diffuse; return this; }
        public Light SetDiffuse(float r, float g, float b) { Diffuse = new Vector3(r,g,b); return this; }
        public Light SetSpecular(Vector3 specular) { Specular = specular; return this; }
        public Light SetSpecular(float r, float g, float b) { Specular = new Vector3(r,g,b); return this; }
        public Light SetAttenuation(Vector3 attenuation) { Attenuation = attenuation; return this; }
        public Light SetAttenuation(float constant, float linear, float quadratic) { Attenuation = new Vector3(constant,linear,quadratic); return this; }
    }

}