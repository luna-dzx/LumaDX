using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LumaDX;


public struct GlState
{
    public bool DepthTest;
    public bool DepthMask;
    public DepthFunction DepthFunc;

    public bool DoCulling;
    public CullFaceMode CullFace;

    public Color4 ClearColor;
    public ClearBufferMask ClearBuffers;

    public bool Blending;
    public BlendingFactor BlendSrc;
    public BlendingFactor BlendDst;


    public GlState()
    {
        DepthTest = true;
        DepthMask = true;
        DepthFunc = DepthFunction.Less;

        DoCulling = true;
        CullFace = CullFaceMode.Back;

        ClearColor = Color4.Black;
        ClearBuffers = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;

        Blending = false;
        BlendSrc = BlendingFactor.SrcAlpha;
        BlendDst = BlendingFactor.OneMinusSrcAlpha;
    }
}


/// <summary>
/// A way of keeping track of the OpenGL state instead of just using the GL functions directly.
/// Also, this allows you to set the OpenGL state as if you're changing variables which feels more intuitive than calling functions.
/// </summary>
public class StateHandler
{

    public GlState ActiveState;
    public GlState StoredState;

    public StateHandler() : this(new GlState()) { }
    public StateHandler(GlState state) { LoadState(state); }

    public void LoadState(GlState state)
    {
        ActiveState = state;
        DepthTest = ActiveState.DepthTest;
        DepthMask = ActiveState.DepthMask;
        DepthFunc = ActiveState.DepthFunc;
        
        DoCulling = ActiveState.DoCulling;
        CullFace = ActiveState.CullFace;

        ClearColor = ActiveState.ClearColor;
        ClearBuffers = ActiveState.ClearBuffers;

        Blending = ActiveState.Blending;
        BlendSrc = ActiveState.BlendSrc;
        BlendDst = ActiveState.BlendDst;
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    public void LoadState() => LoadState(StoredState);
    public void SaveState() => StoredState = ActiveState;

    public bool DepthTest
    {
        set
        {
            ActiveState.DepthTest = value;
            if (value) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);
        }

        get => ActiveState.DepthTest;
    }
    
    public bool DepthMask
    {
        set
        {
            ActiveState.DepthMask = value;
            GL.DepthMask(value);
        }

        get => ActiveState.DepthMask;
        
    }

    public DepthFunction DepthFunc
    {
        set
        {
            ActiveState.DepthFunc = value;
            GL.DepthFunc(value);
        }

        get => ActiveState.DepthFunc;
    }
    
    
    public bool DoCulling
    {
        set
        {
            ActiveState.DoCulling = value;
            if (value) GL.Enable(EnableCap.CullFace);
            else GL.Disable(EnableCap.CullFace);
        }

        get => ActiveState.DoCulling;
    }

    public CullFaceMode CullFace
    {
        set
        {
            ActiveState.CullFace = value;
            GL.CullFace(value);
        }
        get => ActiveState.CullFace;
    }
    
    
    public Color4 ClearColor
    {
        set
        {
            ActiveState.ClearColor = value;
            GL.ClearColor(ActiveState.ClearColor);
        }

        get => ActiveState.ClearColor;
    }    
    
    public ClearBufferMask ClearBuffers
    {
        set => ActiveState.ClearBuffers = value;
        get => ActiveState.ClearBuffers;
    }

    public void Clear() {GL.Clear(ClearBuffers);}

    public bool Blending
    {
        set
        {
            ActiveState.Blending = value;
            if (value) GL.Enable(EnableCap.Blend);
            else GL.Disable(EnableCap.Blend);
        }
        get => ActiveState.Blending;
    }


    public BlendingFactor BlendSrc
    {
        set
        {
            GL.BlendFunc(value,ActiveState.BlendDst);
            ActiveState.BlendSrc = value;
        }
        get => ActiveState.BlendSrc;
    }
    
    public BlendingFactor BlendDst
    {
        set
        {
            GL.BlendFunc(ActiveState.BlendSrc,value);
            ActiveState.BlendDst = value;
        }
        get => ActiveState.BlendDst;
    }

    public void BlendFunc(BlendingFactor sFactor, BlendingFactor dFactor)
    {
        BlendSrc = sFactor;
        BlendDst = dFactor;
    }



}