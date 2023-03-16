// --------------- FlattenedRender/fragment.glsl --------------- //

#version luma-dx

in vec3 normal;
in vec3 colour;

void main()
{
    // don't render anything facing away from camera
    if (dot(normalize(normal), vec3(0.0,0.0,-1.0)) > 0.0) discard;
    lx_FragColour = vec4(colour, 1.0);
}