using ImGuiNET;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector2 = System.Numerics.Vector2;

// https://github.com/NogginBops/ImGui.NET_OpenTK_Sample

namespace LumaDX
{
    public class ImGuiController : IDisposable
    {
        private VertexArray vao;
        private int _vertexBuffer;
        private int _vertexBufferSize;
        private int _indexBuffer;
        private int _indexBufferSize;

        private Texture _fontTexture;
        private ShaderProgram _shader;
        private GameWindow _window; // reference to window in use

        private Vector2 _scaleFactor = Vector2.One;
        
        private Queue<char> _inputBuffer;
        private ImGuiIOPtr io;

        public void SetWindow(ref GameWindow window) => _window = window;
        

        /// <summary>
        /// Constructs a new ImGuiController.
        /// </summary>
        public ImGuiController(GameWindow window) // (objects are passed by reference)
        {
            #region ImGui Context
            
            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            io = ImGui.GetIO();
            io.Fonts.AddFontDefault();

            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            
            #endregion

            #region Window Hooks
            
            _window = window;
            _inputBuffer = new Queue<char>();
            
            _window.TextInput += (e) => _inputBuffer.Enqueue((char)e.Unicode);
            _window.MouseWheel += (e) =>
            {
                io.MouseWheel = e.Offset.Y;
                io.MouseWheelH = e.Offset.X;
            };
            
            #endregion

            #region OpenGL Resources

            _vertexBufferSize = 10000;
            _indexBufferSize = 2000;
            
            _shader = new ShaderProgram(Constants.LibraryShaderPath + "ImGUI/vertex.glsl", Constants.LibraryShaderPath + "ImGUI/fragment.glsl");

            #region Texture

            // Get Texture
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out _);

            int levels = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

            // Save State
            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

            
            // Create OpenGL Texture
            _fontTexture = new Texture(0)
                .LoadPtrMipMapped(pixels,width,height,levels, pixelFormat:PixelFormat.Bgra)
                .Wrapping(TextureWrapMode.Repeat)
                .Filter(Texture.TextureFilter.Linear)
                .MaxLevel(levels-1);
            

            // Restore state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);

            // Attach OpenGL Texture to ImGui
            io.Fonts.SetTexID((IntPtr)_fontTexture.GetHandle());
            io.Fonts.ClearTexData();
            
            #endregion

            #region Vertex Array
            
            // TODO: better VAO handling so that these can be automatically created and deleted, etc.
            
            vao = new VertexArray(BufferUsageHint.DynamicDraw);

            _vertexBuffer = vao.EmptyBuffer(_vertexBufferSize, BufferTarget.ArrayBuffer);
            _indexBufferSize = vao.EmptyBuffer(_indexBufferSize, BufferTarget.ElementArrayBuffer);
            
            int stride = 2 * sizeof(float) + 2 * sizeof(float) + 4 * sizeof(byte);

            vao.SetupBuffer(0, VertexAttribPointerType.Float, 2, stride);
            vao.SetupBuffer(1, VertexAttribPointerType.Float, 2, stride, 8);
            vao.SetupBuffer(2, VertexAttribPointerType.UnsignedByte, 4, stride, 16, true);
            
            #endregion
            
            #endregion

            #region GLFW -> ImGui Key Mapping
            
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Backspace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape;
            io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z;

            #endregion

            
            UpdateImGui(1f / 60f);

            ImGui.NewFrame();
            ImGui.Render();
        }


        public void Render()
        {
            ImGui.Render();
            ImDrawDataPtr drawData = ImGui.GetDrawData();
            
            if (drawData.CmdListsCount == 0) return;

            #region Save State
            
            // Get intial state.
            int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
            int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
            int prevProgram = GL.GetInteger(GetPName.CurrentProgram);
            bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
            bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
            int prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
            int prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
            int prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
            int prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
            int prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
            int prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
            bool prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
            bool prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
            Span<int> prevScissorBox = stackalloc int[4];
            unsafe
            {
                fixed (int* iptr = &prevScissorBox[0])
                {
                    GL.GetInteger(GetPName.ScissorBox, iptr);
                }
            }
            
            #endregion

            #region Resize Buffers
            
            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                ImDrawListPtr cmdList = drawData.CmdListsRange[i];

                int vertexSize = cmdList.VtxBuffer.Size * Constants.ImGuiVertSize;
                if (vertexSize > _vertexBufferSize)
                {
                    int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);
                    
                    vao.EmptyBuffer(newSize, BufferTarget.ArrayBuffer, _vertexBuffer);
                    _vertexBufferSize = newSize;
                }

                int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
                if (indexSize > _indexBufferSize)
                {
                    int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);

                    vao.ResizeIndexBuffer(newSize,_vertexBuffer);
                    _indexBufferSize = newSize;

                }
            }
            
            #endregion
            
            // uniforms
            _shader.Uniform2("screenSize", io.DisplaySize.X,io.DisplaySize.Y);
            _shader.Uniform1("fontTex", 0);

            #region Render Setup
            
            drawData.ScaleClipRects(io.DisplayFramebufferScale);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ScissorTest);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            
            // buffer for rendering ImGui commands with
            vao.Use();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            
            #endregion

            #region ImGui Render Commands
            
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr currentAction = drawData.CmdListsRange[n];

                // Load ImGui Vertex Data to GPU
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, currentAction.VtxBuffer.Size * Constants.ImGuiVertSize, currentAction.VtxBuffer.Data);
                GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, currentAction.IdxBuffer.Size * sizeof(ushort), currentAction.IdxBuffer.Data);

                // Carry out Render Commands
                for (int i = 0; i < currentAction.CmdBuffer.Size; i++) RenderCommand(currentAction.CmdBuffer[i]);
            }
            
            #endregion
            

            #region Restore State
            
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ScissorTest);
            
            // Reset state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);
            GL.UseProgram(prevProgram);
            GL.BindVertexArray(prevVAO);
            GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
            GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb, (BlendEquationMode)prevBlendEquationAlpha);
            GL.BlendFuncSeparate(
                (BlendingFactorSrc)prevBlendFuncSrcRgb,
                (BlendingFactorDest)prevBlendFuncDstRgb,
                (BlendingFactorSrc)prevBlendFuncSrcAlpha,
                (BlendingFactorDest)prevBlendFuncDstAlpha);
            if (prevBlendEnabled) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
            if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
            if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
            if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
            
            #endregion
            
        }

        /// <summary>
        /// Carries out an ImGui Render Command
        /// </summary>
        private void RenderCommand(ImDrawCmdPtr command)
        {
            // Error from ImGui
            if (command.UserCallback != IntPtr.Zero) throw new NotImplementedException();

            #region Setup
            
            // Use command's texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, (int)command.TextureId);
                    
            // Clip rendering to only this section (only render these pixels)
            var clip = command.ClipRect;
            GL.Scissor(
                // Left:
                (int)clip.X,
                // Bottom:
                _window.Size.Y - (int)clip.W,
                // Width:
                (int)(clip.Z - clip.X),
                // Height:
                (int)(clip.W - clip.Y)
            );
            
            #endregion

            // Render
            GL.DrawElementsBaseVertex(
                PrimitiveType.Triangles,
                
                // indices:
                (int)command.ElemCount,
                DrawElementsType.UnsignedShort,
                (IntPtr)(command.IdxOffset * sizeof(ushort)),
                
                // offset:
                unchecked((int)command.VtxOffset)
            );
        }

        private bool focusQueued = false;

        public bool IsFocused(ImGuiFocusedFlags flags = ImGuiFocusedFlags.AnyWindow) => ImGui.IsWindowFocused(flags);
        public void FocusWindow() => focusQueued = true;

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds)
        {
            io = ImGui.GetIO();
            UpdateImGui(deltaSeconds);

            #region Input
            
            MouseState MouseState = _window.MouseState;
            KeyboardState KeyboardState = _window.KeyboardState;

            io.MouseDown[0] = MouseState[MouseButton.Left];
            io.MouseDown[1] = MouseState[MouseButton.Right];
            io.MouseDown[2] = MouseState[MouseButton.Middle];

            var screenPoint = new Vector2i((int)MouseState.X, (int)MouseState.Y);
            var point = screenPoint;//wnd.PointToClient(screenPoint);
            io.MousePos = new Vector2(point.X, point.Y);
            
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (key == Keys.Unknown)
                {
                    continue;
                }
                io.KeysDown[(int)key] = KeyboardState.IsKeyDown(key);
            }

            int numChars = _inputBuffer.Count; // Loop through / process characters pressed and dequeue
            for (int i = 0; i < numChars; i++) io.AddInputCharacter(_inputBuffer.Dequeue());
            

            io.KeyCtrl = KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl);
            io.KeyAlt = KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt);
            io.KeyShift = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
            io.KeySuper = KeyboardState.IsKeyDown(Keys.LeftSuper) || KeyboardState.IsKeyDown(Keys.RightSuper);
            #endregion
            

            ImGui.NewFrame();
            
            if (focusQueued) // if there is a focus queued, do this next frame
            {
                ImGui.SetWindowFocus();
                focusQueued = false;
            }
        }


        private void UpdateImGui(float deltaSeconds)
        {
            // ivec2/vec2 so needs manual division
            io.DisplaySize = new Vector2( 
                _window.Size.X / _scaleFactor.X,
                _window.Size.Y / _scaleFactor.Y);
            
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds;
        }
        
        public void Dispose()
        {
            _fontTexture.Dispose();
            
            vao.Dispose();
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteBuffer(_indexBuffer);
            
            _shader.Dispose();
        }
    }
}