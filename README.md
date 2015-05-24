# AreaSmoother
Blends adjacent areas of flat color together

## How it works
Area Smoother was born out of wanting to smooth out flat single color areas produced by fractals and blen them into each other.
For each pixel, the algorithm looks for the shortest line between the nearest boundary colors and computes a new color in a linear gradient between the boundary colors and the original color of the pixel.
<br/>In effect it creates gradients between centers of flat color areas.

## Examples
| Original | Smoothed |
| :------: | :------: |
| ![red circle plain](examples/o4.png) | ![red circle smoothed](examples/s4.png) |
| ![circles plain](examples/o2.png)    | ![circles smoothed](examples/s2.png)    |

See the [examples](examples "examples") folder for more examples.