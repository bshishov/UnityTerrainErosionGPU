# UnityTerrainErosionGPU
Hydraulic and thermal erosion implement in Unity using compute shaders.

![UnityTerrainErosionGPU](https://raw.githubusercontent.com/bshishov/UnityTerrainErosionGPU/master/Resources/screen1.png)
 
This is an example implementation of hydraulic and thermal erosion with shallow water equations. My initial motivation was to implement a game mechanic like in the [From Dust](https://en.wikipedia.org/wiki/From_Dust) game.


## Disclaimer
This project is still in progress and right now only water simulation works as intended. Hydraulic erosion is very unstable in the current state and requires a lot more parameter tweaking and revisiting the actual implementation. Thermal erosion is not implemented yet but it will come shortly. 

## Demo
Just run `Main` Scene and use mouse to draw water. To remove water change `BrushAmount` property in the `CustomTerrain` component to the negative value.

## How it works
I will explain it later in details... :)

## Project structure
 - `Shaders`
   - [`Shaders/Erosion.compute`](https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Erosion.compute) - all computational stuff happening there in form of separate compute kernels (functions) acting like passes and responsible for different things. Look through that file if you are interested in the actual algorithm implementation.
   - [`Shaders/Water.shader`](https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Water.shader) - Surface shader for rendering water plane. In vertex shader vertex positions are updated from state texture and normals are computed. It has basic lighting and alpha decay depending on depth.
   - [`Shaders/Surface.shader`](https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/Surface.shader) - A lit shader to render the terrain surface. In vertex shader vertex positions are updated from state texture and normals are computed.
   - [`Shaders/InitHeightmap.shader`](https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Shaders/InitHeightmap.shader) - A special shader to initialize initial state from common grayscale heightmap texture. Since state texture is a float texture and operates with values higher than 1 the original heightmap texture should be scaled. This shader is used in the special material used in `CustomTerrain.cs`.
 - [`Scripts/CustomTerrain.cs`](https://github.com/bshishov/UnityTerrainErosionGPU/blob/master/Assets/Scripts/CustomTerrain.cs) - main Monobehavior responsible for mesh creation, compute shader setup, dispatching computation to the GPU, texture creation and parameter sharing.

## References
- Mei, Xing, Philippe Decaudin, and Bao-Gang Hu. "**Fast hydraulic erosion simulation and visualization on GPU.**" 15th Pacific Conference on Computer Graphics and Applications (PG'07). IEEE, 2007. http://www.nlpr.ia.ac.cn/2007papers/gjhy/gh116.pdf
- Jákó, Balázs, and Balázs Tóth. "**Fast Hydraulic and Thermal Erosion on GPU.**" Eurographics (Short Papers). 2011. http://old.cescg.org/CESCG-2011/papers/TUBudapest-Jako-Balazs.pdf  
  *Warning: poor paper quality along with math mistakes. But has nice ideas.*
- Št'ava, Ondřej, et al. "**Interactive terrain modeling using hydraulic erosion.**" Proceedings of the 2008 ACM SIGGRAPH/Eurographics Symposium on Computer Animation. Eurographics Association, 2008. http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.173.5239&rep=rep1&type=pdf


## TODO:
- Better explanation of the implementation
- More descriptive comments in code
- Thermal erosion pipe model
- Improve stability of hydraulic erosion
- Quality of life:
  - Better editor - camera controls and better brush controls
  - Different initial state loaders (from terrain data, from 16bit textures, from `.raw`)
  - Terrain chunks to simplify rendering of distant terrain parts
