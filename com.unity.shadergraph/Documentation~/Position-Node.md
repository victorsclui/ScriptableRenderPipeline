# Position Node

## Description

Provides access to the mesh vertex or fragment's **Position**, depending on the effective [Shader Stage](Shader-Stage.md) of the graph section the [Node](Node.md) is part of. The coordinate space of the output value can be selected with the **Space** dropdown parameter.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 3 | None | **Position** for the Mesh Vertex/Fragment. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Space | Dropdown | Object, View, World, Tangent, Absolute World | Selects coordinate space of **Position** to output. |

## World and Absolute World
The Position node provides drop down options for both **World** space position and **Absolute World** space position. The **Absolute World** option always returns the absolute world position of the object in the scene for all Scriptable Render Pipelines. The **World** option returns the default world space of the selected Scriptable Render Pipeline. 

The [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/index.html) uses [Camera Relative](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering.html) as its default world space. 

The [Lightweight Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.lightweight@latest?preview=1&subfolder=/manual/index.html) uses **Absolute World** as its default world space.

### Upgrading from Previous Versions
If you are using a **Position** node in **World** space on a graph that was authored in version 6.7.0 of the Shader Graph or earlier, the selection will be automatically upgraded to **Absolute World**. This ensures that the calculations on your graph remain accurate to your expectations, as there is a possibility that the output of **World** has changed. 

If you are using a **Position** node in **World** space in the High Definition Render Pipeline to manually calculate [Camera Relative](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering.html) world space, you can now change your node from **Absolute World** to **World** to use [Camera Relative](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering.html) world space out of the box. 