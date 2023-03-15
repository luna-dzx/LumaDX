using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LumaDX;

public abstract class Game : IDisposable
{
    protected static GameWindow Window;

    /// <summary>
    /// Callback for when OpenGL errors happen to output detailed error info at the time which the error initially happens.
    /// Otherwise, the error can be called after subsequent OpenGL calls and it becomes hard to locate the error
    /// </summary>
    private static void DebugCallback(DebugSource source,
        DebugType type,
        int id,
        DebugSeverity severity,
        int length,
        IntPtr message,
        IntPtr userParam)
    {
        string messageString = Marshal.PtrToStringAnsi(message, length);

        Console.WriteLine($"{severity} {type} | {messageString}");

        if (type == DebugType.DebugTypeError)
        {
            Console.WriteLine("Error");
            Window.CursorState = CursorState.Normal;
            throw new Exception();
        }
    }
    
    private static DebugProc _debugProcCallback = DebugCallback;
    private static GCHandle _debugProcCallbackHandle;

    public bool MouseLocked = false;

    /// <summary>
    /// Lock mouse to window and therefore start moving player mouse pos
    /// </summary>
    public void LockMouse()
    {
        MouseLocked = true;
        Window.CursorState = CursorState.Grabbed;
    }
    
    /// <summary>
    /// Unlock mouse from window and therefore stop moving player mouse pos
    /// </summary>
    public void UnlockMouse()
    {
        MouseLocked = false;
        Window.CursorState = CursorState.Normal;
    }

    public int FrameCount;
    

    /// <summary>
    /// Create a new window based on settings
    /// </summary>
    /// <param name="gameWindowSettings"></param>
    /// <param name="nativeWindowSettings"></param>
    /// <param name="debugging">whether to attach the more detailed debug callback to OpenGLs error output</param>
    /// <returns>window object for ease of use</returns>
    public GameWindow InitWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, bool debugging = true, VSyncMode vsync = VSyncMode.On)
    {
        Window = new GameWindow(gameWindowSettings, nativeWindowSettings);
        SetFunctions();

        Window.VSync = vsync;

        if (debugging)
        {
            _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);

            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
        }

        return Window;
    }

    /// <summary>
    /// Start the window as well as the game functions (such as the main render loop)
    /// </summary>
    public void Run()
    {
        if (Window == null)
        {
            Window = new GameWindow(GameWindowSettings.Default, NativeWindowSettings.Default);
            Debug.WriteLine("No window set, defaulting to preset window");
        }
        Window.Run();
    }

    /// <summary>
    /// Free up the resources used by the window when it was open
    /// </summary>
    public void Dispose()
    {
        Window?.Dispose();
        GC.SuppressFinalize(this);
    }
    
    protected Vector2 startMousePos = Vector2.Zero;

    /// <summary>
    /// Set the mouse origin for relative mouse movements
    /// </summary>
    public void SetMouseOrigin() => startMousePos = Window.MousePosition;
    
    /// <summary>
    /// Get the mouse pos relative to an origin (default origin is the start mouse pos upon window creation)
    /// </summary>
    /// <returns>mouse position</returns>
    public Vector2 GetRelativeMouse() => Window.MousePosition - startMousePos;
    
    Vector2 playerMousePos = Vector2.Zero;
    public Vector2 GetPlayerMousePos() => playerMousePos;
    
    
    #region Functions To Override
    
    /// <summary>
    /// Called at program startup before Load. Here you can initialize all of your variables
    /// <para>Initialize() -> <b>Load()</b> -> run</para>
    /// </summary>
    protected virtual void Initialize(){}
    
    /// <summary>
    /// Called at program startup after Initialize. Here you can load up data to the GPU
    /// <para>Initialize() -> <b>Load()</b> -> run</para>
    /// </summary>
    protected virtual void Load(){}

    /// <summary>
    /// Function for binding multiple library functions to the window's load functions
    /// </summary>
    private void Boot()
    {
        Initialize();
        Load();
        SetMouseOrigin();
    }

    /// <summary>
    /// Function for calculating player mouse pos before standard mouse move
    /// </summary>
    private void ExtraMouseMoveFunctions(MouseMoveEventArgs moveInfo)
    {
        if (MouseLocked) { playerMousePos += moveInfo.Delta; }
        MouseMove(moveInfo);
    }
    
    
    /// <summary>
    /// Called before destroying the window
    /// <para><b>Unload()</b> -> destroy window -> end program</para>
    /// </summary>
    protected virtual void Unload(){}
    
    /// <summary>
    /// Called during the update loop after updating to render using OpenGL
    /// <para>update frame -> <b>RenderFrame()</b></para>
    /// </summary>
    /// <param name="args">contains delta time</param>
    protected virtual void RenderFrame(FrameEventArgs args){}
    
    /// <summary>
    /// Called during the update loop before rendering for calculations
    /// <para><b>UpdateFrame()</b> -> render frame</para>
    /// </summary>
    /// <param name="args">contains delta time</param>
    protected virtual void UpdateFrame(FrameEventArgs args){}
    
    /// <summary>
    /// If using multithreading, called on creation of new threads
    /// <para>Handle other threads in this function</para>
    /// </summary>
    protected virtual void RenderThreadStarted(){}

    /// <summary>
    /// Called during final execution before ending the program
    /// <para>unload -> <b>DestroyWindow()</b> -> end program</para>
    /// </summary>
    /// <param name="cancelInfo">get/set whether this event should be cancelled</param>
    protected virtual void DestroyWindow(CancelEventArgs cancelInfo) {}

    /// <summary>
    /// Called upon maximising the window
    /// </summary>
    /// <param name="args">contains whether the window is maximized</param>
    protected virtual void Maximized(MaximizedEventArgs args) {}
    
    /// <summary>
    /// Called upon minimizing the window
    /// </summary>
    /// <param name="args">contains whether the window is minimized</param>
    protected virtual void Minimized(MinimizedEventArgs args){}
    
    /// <summary>
    /// Called upon moving the window
    /// </summary>
    /// <param name="newPosition">the new window position after moving</param>
    protected virtual void Move(WindowPositionEventArgs newPosition){}
    
    /// <summary>
    /// Called when the window refreshes
    /// </summary>
    protected virtual void Refresh(){}
    
    /// <summary>
    /// Called upon resizing the window
    /// </summary>
    /// <param name="newWin">the new window size after moving</param>
    protected virtual void Resize(ResizeEventArgs newWin){}
    
    /// <summary>
    /// Called when files are dropped onto the window
    /// </summary>
    /// <param name="files">contains the file names of the files dropped onto the window</param>
    protected virtual void FileDrop(FileDropEventArgs files){}
    
    /// <summary>
    /// Called when clicking on/off the game window
    /// </summary>
    /// <param name="args">contains whether or not this window is focussed</param>
    protected virtual void FocusedChanged(FocusedChangedEventArgs args){}
    
    /// <summary>
    /// Called upon connecting a joystick to the computer
    /// </summary>
    /// <param name="args">information about the connected joystick</param>
    protected virtual void JoystickConnected(JoystickEventArgs args){}
    
    /// <summary>
    /// Activates similar to typing when holding a key
    /// <para>start -> pause -> repeat</para>
    /// </summary>
    /// <param name="keyInfo">information about which key was pressed</param>
    protected virtual void KeyDown(KeyboardKeyEventArgs keyInfo){}

    /// <summary>
    /// Called every frame for processing keyboard game inputs
    /// </summary>
    /// <param name="args">contains delta time</param>
    /// <param name="keyboardState">information about which keys are pressed</param>
    protected virtual void KeyboardHandling(FrameEventArgs args, KeyboardState keyboardState) {}

    /// <summary>
    /// Called only on the frame when a key is released
    /// </summary>
    /// <param name="args">information about which key was released</param>
    protected virtual void KeyUp(KeyboardKeyEventArgs args) {}
    
    /// <summary>
    /// Called only on the frame when a mouse button is pressed
    /// </summary>
    /// <param name="args">information about which mouse button was pressed</param>
    protected virtual void MouseButton(MouseButtonEventArgs args){}

    /// <summary>
    /// Called every frame for processing mouse game inputs
    /// </summary>
    /// <param name="args">contains delta time</param>
    /// <param name="mouseState">information about the mouse</param>
    protected virtual void MouseHandling(FrameEventArgs args, MouseState mouseState) {}
    
    /// <summary>
    /// Called only on the frame where the mouse moves from being off the screen to being on the screen
    /// </summary>
    protected virtual void MouseEnter(){}
    
    /// <summary>
    /// Called only on the frame where the mouse moves from being on the screen to being off the screen
    /// </summary>
    protected virtual void MouseLeave(){}
    
    /// <summary>
    /// Called any frame where the mouse is in a different location from the last
    /// </summary>
    /// <param name="moveInfo">the mouse's new position and what vector it moved by</param>
    protected virtual void MouseMove(MouseMoveEventArgs moveInfo){}
    
    /// <summary>
    /// Called only on the frame when the mouse is released
    /// </summary>
    /// <param name="args">information about which mouse button was released</param>
    protected virtual void MouseUp(MouseButtonEventArgs args){}
    
    /// <summary>
    /// Called only on the frame when a scroll action is inputted
    /// </summary>
    /// <param name="scroll">information about the scroll input</param>
    /// <remarks>accounts for scrolling by >1 per frame (in <b>scroll</b>)</remarks>
    protected virtual void MouseScroll(MouseWheelEventArgs scroll){}
    
    /// <summary>
    /// Called on typing event, formats the text for you
    /// </summary>
    /// <param name="text">formatted input from this frame</param>
    protected virtual void TextInput(TextInputEventArgs text){}

    /// <summary>
    /// Called every frame to add additional functions besides the standard OpenTK functions
    /// </summary>
    /// <param name="args">contains delta time</param>
    private void ExtraPerFrameFunctions(FrameEventArgs args)
    {
        MouseLocked = Window.CursorState == CursorState.Grabbed;
        KeyboardHandling(args,Window.KeyboardState);
        MouseHandling(args,Window.MouseState);
        FrameCount++;
    }

    /// <summary>
    /// Fixes resizing to also adjust the viewport for rendering
    /// </summary>
    private void AdjustViewport(ResizeEventArgs newSize)
    {
        GL.Viewport(0,0,newSize.Size.X,newSize.Size.Y);
    }

    /// <summary>
    /// Adds the functions from the <b>Game</b> class to the OpenTK window
    /// </summary>
    private void SetFunctions()
    {
        Window.Load += Boot;
        Window.Unload += Unload;
        Window.RenderFrame += RenderFrame;
        Window.UpdateFrame += ExtraPerFrameFunctions;
        Window.UpdateFrame += UpdateFrame;
        Window.RenderThreadStarted += RenderThreadStarted;
        Window.Closing += DestroyWindow;
        Window.Maximized += Maximized;
        Window.Minimized += Minimized;
        Window.Move += Move;
        Window.Refresh += Refresh;
        Window.Resize += AdjustViewport;
        Window.Resize += Resize;
        Window.FileDrop += FileDrop;
        Window.FocusedChanged += FocusedChanged;
        Window.JoystickConnected += JoystickConnected;
        Window.KeyDown += KeyDown;
        Window.KeyUp += KeyUp;
        Window.MouseDown += MouseButton;
        Window.MouseEnter += MouseEnter;
        Window.MouseLeave += MouseLeave;
        Window.MouseMove += ExtraMouseMoveFunctions;
        Window.MouseUp += MouseUp;
        Window.MouseWheel += MouseScroll;
        Window.TextInput += TextInput;
    }
    
    #endregion

}