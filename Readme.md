# OpenSCAD OpenAI Generator

A Windows desktop app that generates 3D-printable STL files from text descriptions using OpenAI and [OpenSCAD](https://openscad.org/).

Describe what you want ("a 20mm cube with a 5mm hole through the center"), and it generates OpenSCAD code, renders it, and exports an STL file ready for your slicer.

## Requirements

- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- [OpenSCAD](https://openscad.org/downloads.html) installed
- OpenAI API key

## Setup

1. Clone the repo
2. Create a `.env` file in the project root:
   ```
   OPENAI_API_KEY=sk-your-key-here
   ```
3. Build and run:
   ```
   dotnet build
   dotnet run --project "STL Generator"
   ```

### Publish as standalone .exe

```
dotnet publish -c Release -r win-x64 --self-contained
```

Find the output in `bin\Release\net8.0-windows\win-x64\publish\`.

## Usage

1. Launch the app
2. Enter a description of your 3D model
3. Click **Generate OpenSCAD** to get the code
4. Review or edit the code if needed
5. Click **Export STL** to save the model as an STL file

## OpenSCAD Code Generation Rules

- Only 3D objects at the top level (cube, sphere, cylinder, etc.)
- 2D shapes (circle, square, polygon) are always extruded to 3D via `linear_extrude()`
- All required parameters are always specified
- No markdown, echo, assert, or debugging statements in output
- Output is always a complete, valid OpenSCAD script

## Troubleshooting

- **STL export fails:** Make sure OpenSCAD is installed and the path is correct. The model must be 3D.
- **OpenAI errors:** Check your API key and internet connection.

## License

MIT

## Credits

- [OpenAI](https://openai.com/)
- [OpenSCAD](https://openscad.org/)
- [HandyControl](https://github.com/HandyOrg/HandyControl)
