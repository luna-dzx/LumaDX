using System.Reflection;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using static LumaDX.Objects;

namespace LumaDX;

public class Shader : IDisposable
{
    public readonly int ID;

    /// <summary>
    /// Compile and store shader
    /// </summary>
    /// <param name="text">glsl code in raw text</param>
    /// <param name="type">what type of shader this program is</param>
    public Shader(string text,ShaderType type)
    {
        ID = GL.CreateShader(type);
        GL.ShaderSource(ID,text);
        GL.CompileShader(ID);
        
        ErrorCode error = GL.GetError();
        if (error != ErrorCode.NoError) throw new Exception(error.ToString());
        
        string infoLog = GL.GetShaderInfoLog(ID);
        if (!string.IsNullOrEmpty(infoLog)) throw new Exception(infoLog);
    }

    /// <summary>
    /// Alternative to using constructor to load from a file instead of plaintext
    /// </summary>
    /// <param name="path">path to a file containing glsl code</param>
    /// <param name="type">what type of shader this program is</param>
    /// <returns>the shader which was created from loading this file</returns>
    public static Shader FromFile(string path, ShaderType type) => new Shader(File.ReadAllText(path), type);

    /// <summary>
    /// Override (int) cast to return the ID
    /// </summary>
    /// <param name="shader">the shader to cast</param>
    /// <returns>the shader ID</returns>
    public static explicit operator int(Shader shader) => shader.ID;

    /// <summary>
    /// Delete this shader object
    /// </summary>
    public void Dispose() => GL.DeleteShader(ID);
    

}


public class ShaderProgram : IDisposable
{

    public int NUM_LIGHTS = 64;
    
    private readonly int handle;
    private Dictionary<string, int> uniforms;
    private Dictionary<string, (FieldInfo ,int)> syncedUniforms;

    private Dictionary<ShaderType, List<string>> sections;
    private Dictionary<int, bool> usesCustomSynax;
    private List<int> shaders;

    // only works if using custom syntax on a vertex shader
    private bool autoProjection = false;
    
    // set to true on compilation for checking whether this ShaderProgram object has actually been made yet
    public bool Compiled = false;

    /// <summary>
    /// Enables the automatic calculation of lx_Transform
    /// </summary>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram EnableAutoProjection()
    {
        Uniform1("lx_AutoProjection",1);
        autoProjection = true;
        return this;
    }
    /// <summary>
    /// Disables the automatic calculation of lx_Transform
    /// </summary>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram DisableAutoProjection()
    {
        Uniform1("lx_AutoProjection",0);
        autoProjection = false;
        return this;
    }

    private static (string,string,string) ReadEngineShader(ShaderType type)
    {
        string mainFileDirectory = 
            Constants.LibraryShaderPath +
               (int)type switch
               {
                   35632 => "lxFragment.glsl",
                   35633 => "lxVertex.glsl",
                   36313 => "lxGeometry.glsl",
                   36487 => "lxTessEval.glsl",
                   36488 => "lxTessCtrl.glsl",
                   37305 => "lxCompute.glsl",
                   _ => throw new Exception("Invalid ShaderType")
               };


        string mainFileText = File.ReadAllText(mainFileDirectory);
        
        string postMain = "";
        string[] postSplit = mainFileText.Split("[post-main]");
        if (postSplit.Length > 1) postMain = postSplit[1];

        mainFileText = postSplit[0];
        
        
        string[] splitMainFile = { "", "" };
        if (File.Exists(mainFileDirectory))
            splitMainFile = mainFileText.Split("[main]");
        
        
        string[] splitBaseFile = { "", "" };
        if (File.Exists(Constants.LibraryShaderPath + "lxGlobal.glsl")) 
            splitBaseFile = File.ReadAllText(Constants.LibraryShaderPath + "lxGlobal.glsl").Split("[main]");

        return (splitBaseFile[0]+"\n"+splitMainFile[0],
            (splitBaseFile.Length > 1 ? splitBaseFile[1] : "")+"\n"+(splitMainFile.Length > 1 ? splitMainFile[1] : ""),
            postMain);
    }

    /// <summary>
    /// Uses the engine's custom glsl syntax to format a shader
    /// </summary>
    /// <param name="shaderText">the original shader's text</param>
    /// <param name="shaderType">the type of OpenGL shader to compile this as</param>
    /// <returns>the shader formatted to be compiled as glsl</returns>
    private string FormatShader(string shaderText, ShaderType shaderType)
    {
        string[] lines = shaderText.Split('\n');

        if (lines[0].Length < 16 || lines[0].Substring(9, 7) != "luma-dx") return shaderText;

        usesCustomSynax[(int)shaderType] = true;
        
        var (engineShader,engineShaderMain,postMain) = ReadEngineShader(shaderType);

        lines[0] = "#version 330 core\n#define NUM_LIGHTS "+NUM_LIGHTS+"\n";
        lines[0] += engineShader;
        lines[0] += "\nuniform int active"+shaderType+"Id;\n";
        
        string outputText = "";
        string currentText = "";

        sections[shaderType] = new List<string>();
        // first section contains no main functions, this makes the first section section 0
        int currentId = -1;

        foreach (string line in lines)
        {
            int firstCharIndex = 0;
            int lastCharIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ') continue;
                firstCharIndex = i; break;
            }

            for (int i = line.Length-1; i > -1; i--)
            {
                if (line[i] == ' ') continue;
                lastCharIndex = i; break;
            }

            if (line.Length>0 && line[firstCharIndex] == '[' && line[lastCharIndex] == ']')
            {

                string test = String.ReplaceAll(currentText, "main", "lx_program" + currentId + "_main");
                outputText += test;

                string sectionName = line.Substring(firstCharIndex+1, lastCharIndex - firstCharIndex - 1);
                currentText = "";
                sections[shaderType].Add(sectionName);

                currentId++;
            }
            else
            {
                currentText += line+"\n";
            }
        }


        if (currentId == -1) currentId = 0;// no sections 

        outputText += String.ReplaceAll(currentText, "main", "lx_program" + currentId + "_main");
        
        
        outputText += "\nvoid main(){";
        
        outputText += engineShaderMain;
        
        // shader sections:

        if (sections[shaderType].Count == 0) // no sections
        {
            outputText += "\nlx_program0_main();";
        }
        else
        {
            for (int i = 0; i < sections[shaderType].Count; i++)
            {
                outputText += "\nif (active" + shaderType + "Id == "+i+") {lx_program" + i + "_main();}";
            }
        }
        
        
        
        outputText += "\n"+postMain;
        
        outputText += "\n}";

        return outputText;
    }

    /// <summary>
    /// Create an empty shader program for configuration outside the constructor
    /// </summary>
    public ShaderProgram()
    {
        handle = GL.CreateProgram();
        uniforms = new Dictionary<string, int>();
        syncedUniforms = new Dictionary<string, (FieldInfo, int)>();
        
        usesCustomSynax = new Dictionary<int, bool>
        {
            [(int)ShaderType.ComputeShader] = false,
            [(int)ShaderType.FragmentShader] = false,
            [(int)ShaderType.GeometryShader] = false,
            [(int)ShaderType.VertexShader] = false,
            [(int)ShaderType.FragmentShaderArb] = false,
            [(int)ShaderType.TessControlShader] = false,
            [(int)ShaderType.TessEvaluationShader] = false,
            [(int)ShaderType.VertexShaderArb] = false
        };

        sections = new Dictionary<ShaderType, List<string>>();
        shaders = new List<int>();
    }

    /// <summary>
    /// Create a generic shader program based on a vertex and fragment shader
    /// </summary>
    /// <param name="vertexPath">the path to a glsl vertex shader file</param>
    /// <param name="fragmentPath">the path to a glsl fragment shader file</param>
    /// <param name="useAutoProjection">enables the automatic calculation of lx_Transform</param>
    /// <param name="numLights">the size of lx_Light arrays that you pass to functions</param>
    public ShaderProgram(string vertexPath, string fragmentPath,bool useAutoProjection = false, int numLights = 64) : this()
    {
        SetLightCount(numLights);
        LoadShader(vertexPath, ShaderType.VertexShader);
        LoadShader(fragmentPath, ShaderType.FragmentShader);
        Compile();
        if (useAutoProjection) EnableAutoProjection();
    }


    private const string PostProcessVertexPath = "../../../Library/Shaders/PostProcessing/vertex.glsl";
    
    /// <summary>
    /// Create a post processing shader program from a fragment shader
    /// </summary>
    /// <param name="fragmentPath">the path to a glsl fragment shader file</param>
    /// <param name="useAutoProjection">enables the automatic calculation of lx_Transform</param>
    /// <param name="numLights">the size of lx_Light arrays that you pass to functions</param>
    public ShaderProgram(string fragmentPath, bool useAutoProjection = false, int numLights = 64) : this()
    {
        SetLightCount(numLights);
        LoadPostProcessVertex();
        LoadShader(fragmentPath, ShaderType.FragmentShader);
        Compile();
        if (useAutoProjection) EnableAutoProjection();
    }
    
    

    public ShaderProgram SetLightCount(int numLights)
    {
        NUM_LIGHTS = numLights;
        return this;
    }

    /// <summary>
    /// Assign this shader pipeline for rendering
    /// </summary>
    public ShaderProgram Use()
    {
        GL.UseProgram(handle);
        return this;
    } 

    /// <summary>
    /// Load shader from file then format custom syntax and load to the shader program
    /// </summary>
    /// <param name="path">path to the glsl shader file</param>
    /// <param name="shaderType">the type of shader to use this as in the shader pipeline</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram LoadShader(string path,ShaderType shaderType) => LoadShaderText(File.ReadAllText(path),shaderType);
    
    /// <summary>
    /// Load post processing vertex shader from file then format custom syntax and load to the shader program
    /// </summary>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram LoadPostProcessVertex() => LoadShaderText(File.ReadAllText(PostProcessVertexPath),ShaderType.VertexShader);
        
    /// <summary>
    /// Format custom syntax in a shader (in plaintext) and load to the shader program
    /// </summary>
    /// <param name="shaderText">plaintext glsl shader</param>
    /// <param name="shaderType">the type of shader to use this as in the shader pipeline</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram LoadShaderText(string shaderText,ShaderType shaderType)
    {
        shaderText = FormatShader(shaderText,shaderType);
        Shader shader = new Shader(shaderText, shaderType);
        shaders.Add(shader.ID);
        return this;
    }
        
    /// <summary>
    /// Add an existing shader to the shader program
    /// </summary>
    /// <param name="shaderId">the OpenGL shader handle</param>
    /// <returns>current object for ease of use</returns>
    /// <remarks>cannot get interpreted as a multi-shader</remarks>
    public ShaderProgram AddShader(int shaderId)
    {
        shaders.Add(shaderId);
        return this;
    }

    /// <summary>
    /// Compile any shaders which were loaded before this function call
    /// </summary>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Compile()
    {
        LoadShaders(shaders.ToArray());
        Compiled = true;
        return this;
    }
    
    /// <summary>
    /// Load pre-existing shaders which have already been created
    /// </summary>
    /// <param name="shaderIDs">the OpenGL handles of the shaders</param>
    public void LoadShaders(int[] shaderIDs)
    {
        // attach to program
        foreach (int id in shaderIDs)
        {
            GL.AttachShader(handle,id);
        }
        
        GL.LinkProgram(handle);
        
        
        string error = GL.GetProgramInfoLog(handle);
        if (error!="") throw new Exception(error);
        
        // delete from memory
        foreach (int id in shaderIDs)
        { GL.DetachShader(handle,id); GL.DeleteShader(id); }
    }

    /// <summary>
    /// Set the active shader within a certain multi-shader (based on the shader type)
    /// </summary>
    /// <param name="shader">the type of shader to set</param>
    /// <param name="sectionName">the title of the section that contains the main function of the desired shader to switch to</param>
    /// <returns>current object for ease of use</returns>
    /// <remarks>this cannot work for a standard shader, only ones with #version luma-dx</remarks>
    public ShaderProgram SetActive(ShaderType shader, string sectionName)
    {
        this.Use();
        if (!sections.ContainsKey(shader)) throw new Exception("No multi-shader sections in shader of type "+shader);
        
        GL.Uniform1(
            GL.GetUniformLocation(handle,"active" + shader + "Id"),
            sections[shader].IndexOf(sectionName)
        );

        return this;
    }
    
    
    /// <summary>
    /// Remove shader program from video memory
    /// </summary>
    /// <exception cref="Exception">error code for if deleting fails</exception>
    public void Dispose()
    {
        GL.DeleteProgram(handle);
        ErrorCode error = GL.GetError();
        if (error != ErrorCode.NoError) throw new Exception(error.ToString());
    }

    /// <summary>
    /// Get the shader program's handle
    /// </summary>
    /// <returns>the OpenGL shader program handle</returns>
    public int GetHandle() => handle;
    
    
    /// <summary>
    /// Override (int) cast to return the handle
    /// </summary>
    /// <param name="program">the shader program to cast</param>
    /// <returns>the OpenGL shader program handle</returns>
    public static explicit operator int(ShaderProgram program) => program.GetHandle();
    /// <summary>
    /// Retrieve the binding of a uniform variable in the shader program
    /// </summary>
    /// <param name="name">the uniform variable's name in glsl</param>
    /// <returns>uniform location of the variable</returns>
    /// <exception cref="Exception">OpenGL exception</exception>s
    public int GetUniform(string name)
    {
        this.Use();
        if (!uniforms.ContainsKey(name))uniforms.Add(name,GL.GetUniformLocation(handle,name));
        //ErrorCode error = GL.GetError();
        //if (error != ErrorCode.NoError) throw new Exception(error.ToString());
        return uniforms[name];
    }

    /// <summary>
    /// The uniform location of the standard projection matrix
    /// </summary>
    /// <returns>uniform location of the lx_Proj variable or -1 if the
    /// vertex shader is not configured with custom syntax or if auto projection is disabled</returns>
    public int DefaultProjection
    {
        get
        {
            if (usesCustomSynax[(int)ShaderType.VertexShader] && autoProjection)
            {
                return GL.GetUniformLocation(handle, "lx_Proj");
            }
            
            return -1;
        }

    }

    private int customModelMatrixBinding = -1;

    /// <summary>
    /// The uniform location of the standard model matrix
    /// </summary>
    /// <returns>uniform location of the lx_Model variable or -1 if the
    /// vertex shader is not configured with custom syntax or if auto projection is disabled</returns>
    public int DefaultModel
    {
        get
        {
            if (usesCustomSynax[(int)ShaderType.VertexShader] && autoProjection)
            {
                return GL.GetUniformLocation(handle, "lx_Model");
            }

            return customModelMatrixBinding; // -1 if not set
        }
    }


    public ShaderProgram SetModelLocation(int binding)
    {
        customModelMatrixBinding = binding;
        return this;
    }
    public ShaderProgram SetModelLocation(string name)
    {
        customModelMatrixBinding = GetUniform(name);
        return this;
    }

    /// <summary>
    /// The uniform location of the standard view matrix
    /// </summary>
    /// <returns>uniform location of the lx_View variable or -1 if the
    /// vertex shader is not configured with custom syntax or if auto projection is disabled</returns>
    public int DefaultView
    {
        get
        {
            if (usesCustomSynax[(int)ShaderType.VertexShader] && autoProjection)
            {
                return GL.GetUniformLocation(handle, "lx_View");
            }

            return -1;
        }
    }

    public ShaderProgram EnableGammaCorrection()
    {
        GL.Enable(EnableCap.FramebufferSrgb);
        Uniform1("lx_IsGammaCorrectionEnabled", 1);
        return this;
    }
    public ShaderProgram DisableGammaCorrection()
    {
        GL.Disable(EnableCap.FramebufferSrgb);
        Uniform1("lx_IsGammaCorrectionEnabled", 0);
        return this;
    }



    #region Uniform Functions

    #region 1D uniform
    /// <summary>
    /// Set a uniform variable's value on the gpu
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform1(string name, double v0) { GL.Uniform1(GetUniform(name), v0); return this; }
    /// <summary>
    /// Set a uniform variable's value on the gpu
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform1(string name, float v0) { GL.Uniform1(GetUniform(name), v0); return this; }
    /// <summary>
    /// Set a uniform variable's value on the gpu
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform1(string name, int v0) { GL.Uniform1(GetUniform(name), v0); return this; }
    /// <summary>
    /// Set a uniform variable's value on the gpu
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform1(string name, uint v0) { GL.Uniform1(GetUniform(name), v0); return this; }
        
    #endregion
        
    #region 2D uniform
        
    /// <summary>
    /// Set a 2d uniform variable's value on the gpu (vec2)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="vector">vector value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform2(string name, Vector2 vector) { GL.Uniform2(GetUniform(name), vector); return this; }
    /// <summary>
    /// Set a 2d uniform variable's value on the gpu (vec2)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform2(string name, double v0, double v1) { GL.Uniform2(GetUniform(name), v0,v1); return this; }
    /// <summary>
    /// Set a 2d uniform variable's value on the gpu (vec2)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform2(string name, float v0, float v1) { GL.Uniform2(GetUniform(name), v0,v1); return this; }
    /// <summary>
    /// Set a 2d uniform variable's value on the gpu (vec2)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform2(string name, int v0, int v1) { GL.Uniform2(GetUniform(name), v0,v1); return this; }
    /// <summary>
    /// Set a 2d uniform variable's value on the gpu (vec2)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform2(string name, uint v0, uint v1) { GL.Uniform2(GetUniform(name), v0,v1); return this; }

    public ShaderProgram UniformMat2(string name, ref Matrix2 matrix) { GL.UniformMatrix2(GetUniform(name), false, ref matrix); return this; }
    
    #endregion
        
    #region 3D uniform
        
    /// <summary>
    /// Set a 3d uniform variable's value on the gpu (vec3)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="vector">vector value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform3(string name, Vector3 vector) { GL.Uniform3(GetUniform(name), vector); return this; }
    /// <summary>
    /// Set a 3d uniform variable's value on the gpu (vec3)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <param name="v2">z component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform3(string name, double v0, double v1, double v2) { GL.Uniform3(GetUniform(name), v0,v1,v2); return this; }
    /// <summary>
    /// Set a 3d uniform variable's value on the gpu (vec3)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <param name="v2">z component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform3(string name, float v0, float v1, float v2) { GL.Uniform3(GetUniform(name), v0,v1,v2); return this; }
    /// <summary>
    /// Set a 3d uniform variable's value on the gpu (vec3)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <param name="v2">z component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform3(string name, int v0, int v1, int v2) { GL.Uniform3(GetUniform(name), v0,v1,v2); return this; }
    /// <summary>
    /// Set a 3d uniform variable's value on the gpu (vec3)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <param name="v2">z component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform3(string name, uint v0, uint v1, uint v2) { GL.Uniform3(GetUniform(name), v0,v1,v2); return this; }
        
    #endregion
        
    #region 4D uniform
        
    /// <summary>
    /// Set a 4d uniform variable's value on the gpu (vec4)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="vector">vector value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform4(string name, Vector4 vector) { GL.Uniform4(GetUniform(name), vector); return this; }
    /// <summary>
    /// Set a 4d uniform variable's value on the gpu (vec4)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <param name="v2">z component value</param>
    /// <param name="v3">w component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform4(string name, double v0, double v1, double v2, double v3) { GL.Uniform4(GetUniform(name), v0,v1,v2,v3); return this; }
    /// <summary>
    /// Set a 4d uniform variable's value on the gpu (vec4)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <param name="v2">z component value</param>
    /// <param name="v3">w component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform4(string name, float v0, float v1, float v2, float v3) { GL.Uniform4(GetUniform(name), v0,v1,v2,v3); return this; }
    /// <summary>
    /// Set a 4d uniform variable's value on the gpu (vec4)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <param name="v2">z component value</param>
    /// <param name="v3">w component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform4(string name, int v0, int v1, int v2, int v3) { GL.Uniform4(GetUniform(name), v0,v1,v2,v3); return this; }
    /// <summary>
    /// Set a 4d uniform variable's value on the gpu (vec4)
    /// </summary>
    /// <param name="name">the uniform variable's name</param>
    /// <param name="v0">x component value</param>
    /// <param name="v1">y component value</param>
    /// <param name="v2">z component value</param>
    /// <param name="v3">w component value</param>
    /// <returns>current object for ease of use</returns>
    public ShaderProgram Uniform4(string name, uint v0, uint v1, uint v2, uint v3) { GL.Uniform4(GetUniform(name), v0,v1,v2,v3); return this; }
    
    public ShaderProgram UniformMat4(string name, ref Matrix4 matrix) { GL.UniformMatrix4(GetUniform(name),false,ref matrix); return this; }

    #endregion


    #region Uniform Arrays

    public ShaderProgram UniformMat4Array(string name, ref Matrix4[] matrices)
    {
        for (int i = 0; i < matrices.Length; i++)
        {
            GL.UniformMatrix4(GetUniform(name+"["+i+"]"),false,ref matrices[i]);
        }

        return this;
    }
    
    public ShaderProgram UniformVec3Array(string name, Vector3[] vectors)
    {
        for (int i = 0; i < vectors.Length; i++)
        {
            GL.Uniform3(GetUniform(name+"["+i+"]"),vectors[i]);
        }

        return this;
    }
    

    #endregion
        
    #region Engine Specific

    public ShaderProgram UniformMaterial(string name, Material material)
    {
        Uniform3(name + ".ambient", material.Ambient);
        Uniform3(name + ".diffuse", material.Diffuse);
        Uniform3(name + ".specular", material.Specular);
        Uniform1(name + ".shininess", material.Shininess);
        return this;
    }
    
    public ShaderProgram UniformMaterial(string name, Material material, Texture texture)
    {
        Uniform3(name + ".ambient", material.Ambient);
        Uniform3(name + ".diffuse", material.Diffuse);
        Uniform3(name + ".specular", material.Specular);
        Uniform1(name + ".shininess", material.Shininess);
        texture.Uniform(this, name+".specTex");
        return this;
    }
    
    public ShaderProgram UniformMaterial(string name, Material material, int textureHandle, int textureUnit = 0, 
        TextureTarget textureTarget = TextureTarget.Texture2D)
    {
        Uniform3(name + ".ambient", material.Ambient);
        Uniform3(name + ".diffuse", material.Diffuse);
        Uniform3(name + ".specular", material.Specular);
        Uniform1(name + ".shininess", material.Shininess);
        
        GL.ActiveTexture((TextureUnit) (textureUnit + (int)TextureUnit.Texture0));
        GL.BindTexture(textureTarget,textureHandle);

        GL.UseProgram((int)this);
        GL.Uniform1(GL.GetUniformLocation((int)this,name+".baseTex"),textureUnit);
        return this;
    }
    
    public ShaderProgram UniformMaterial(string name, Material material, Texture texture, Texture textureSpecular)
    {
        Uniform3(name + ".ambient", material.Ambient);
        Uniform3(name + ".diffuse", material.Diffuse);
        Uniform3(name + ".specular", material.Specular);
        Uniform1(name + ".shininess", material.Shininess);
        texture.Uniform(this, name+".baseTex");
        textureSpecular.Uniform(this, name+".specTex");
        return this;
    }
    
    public ShaderProgram UniformMaterial(string name, Material material, int baseTexHandle, int specTexHandle, int baseTexUnit = 0, int specTexUnit = 1,
        TextureTarget baseTexTarget = TextureTarget.Texture2D, TextureTarget specTexTarget = TextureTarget.Texture2D)
    {
        Uniform3(name + ".ambient", material.Ambient);
        Uniform3(name + ".diffuse", material.Diffuse);
        Uniform3(name + ".specular", material.Specular);
        Uniform1(name + ".shininess", material.Shininess);
        
        GL.ActiveTexture((TextureUnit) (baseTexUnit + (int)TextureUnit.Texture0));
        GL.BindTexture(baseTexTarget,baseTexHandle);

        GL.UseProgram((int)this);
        GL.Uniform1(GL.GetUniformLocation((int)this,name+".baseTex"),baseTexUnit);

        
        GL.ActiveTexture((TextureUnit) (specTexUnit + (int)TextureUnit.Texture0));
        GL.BindTexture(specTexTarget,specTexHandle);

        GL.UseProgram((int)this);
        GL.Uniform1(GL.GetUniformLocation((int)this,name+".specTex"),specTexUnit);

        return this;
    }
    

    public ShaderProgram UniformLight(string name, Light light)
    {
        Uniform3(name + ".position", light.Position);
        Uniform3(name + ".direction", light.Direction);
        Uniform1(name + ".cutOff", light.GetCutOff());
        Uniform1(name + ".outerCutOff", light.GetOuterCutOff());
        Uniform3(name + ".ambient", light.Ambient);
        Uniform3(name + ".diffuse", light.Diffuse);
        Uniform3(name + ".specular", light.Specular);
        Uniform3(name + ".attenuation", light.Attenuation);
        return this;
    }
    
    public ShaderProgram UniformLightArray(string name, Light[] lights)
    {
        for (int i = 0; i < lights.Length; i++)
        {
            UniformLight(name+"[" + i + "]", lights[i]);
        }
        return this;
    }

    // improve this whole thing pls
    public ShaderProgram UniformTexture(string name, Texture texture)
    {
        texture.Uniform(this, name);
        return this;
    }

    #endregion
        
    #endregion
    
    #region Synced Uniforms
    
    // NOTE: using these isn't very good practice and isn't very efficient, however for small projects
    // where maximum efficiency isn't necessary they can sometimes slightly reduce the programming workload

    
    /// <summary>
    /// Use reflections to sync variables between C# and glsl based on their names - only supports simple vectors and scalars
    /// </summary>
    /// <param name="name">variable name</param>
    /// <param name="game">game class containing the variable</param>
    public void SyncUniform(string name, Game game)
    {
        syncedUniforms[name] = (
            game.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance),
            GL.GetUniformLocation(handle,name)
        )!;
    }

    /// <summary>
    /// Loads all synced uniforms to the GPU
    /// </summary>
    /// <param name="game">game class containing the synced uniforms</param>
    public void UpdateSyncedUniforms(Game game)
    {
        foreach (string name in syncedUniforms.Keys)
        {
            UpdateSyncedUniform(game, name);
        }
    }

    /// <summary>
    /// Loads specific synced uniform to the GPU
    /// </summary>
    /// <param name="game">game class containing the synced uniform</param>
    /// <param name="name">the name of the specific synced uniform</param>
    /// <exception cref="Exception">unsupported synced uniform type</exception>
    public void UpdateSyncedUniform(Game game, string name)
    {
        this.Use();
        var (variable, uniformId) = syncedUniforms[name];
        object value = variable.GetValue(game)!;
        Type t = value.GetType();
        
        #region scalars
        // Uniform 1
        if (t == typeof(float)) { GL.Uniform1(uniformId,(float)value); return;}
        if (t == typeof(int)) { GL.Uniform1(uniformId,(int)value); return;}
        if (t == typeof(uint)) { GL.Uniform1(uniformId,(uint)value); return;}
        if (t == typeof(double)) { GL.Uniform1(uniformId,(double)value); return;}
        #endregion
        
        #region vectors
        // Uniform 2
        if (t == typeof(Vector2)) { GL.Uniform2(uniformId,(Vector2)value); return;}
        // Uniform 3
        if (t == typeof(Vector3)) { GL.Uniform3(uniformId,(Vector3)value); return;}
        // Uniform 4
        if (t == typeof(Vector4)) { GL.Uniform4(uniformId,(Vector4)value); return;}
        #endregion

        throw new Exception("Invalid synced uniform type");
    }
    
    #endregion


}