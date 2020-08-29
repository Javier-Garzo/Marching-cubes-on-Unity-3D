Shader "Custom/ColorCullShader"
{
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" { }
    }
        SubShader{
        // Render the front-facing parts of the object.
        // We use a simple white material, and apply the main texture.
        Pass {
            Color(1,0.2,0.2,1)
            Material {
                Diffuse(1,0,0,1)
            }
            Lighting Off
            SetTexture[_MainTex] {
                Combine Primary * Texture
            }
        }

        // Now we render the back-facing triangles with blue
        Pass {
            Color(0,0.5,1,1)
            Cull Front
        }
    }
}