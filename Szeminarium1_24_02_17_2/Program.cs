using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static ImGuiController controller;

        private static GlCube skyBox;

        private static List<GlCube> platformCubes = new();

        private static List<GlArrow> arrows = new();

        private static List<MovingArrow> movingArrows = new();
        private static Random rng = new();

        private static double spawnCooldown = 1.5;
        private static double timeSinceLastSpawn = 0.0;

        private static List<(MovingArrow arrow, float scale)> shrinkingArrows = new();
        private const float shrinkSpeed = 1.5f;

        private static bool isFirstPersonView = false;
        private static GlObject fish;

        // current pos
        private static int fishX = 1; 
        private static int fishZ = 1;

        // target pos
        private static int fishTargetX = 1;                     
        private static int fishTargetZ = 1;

        private static bool fishIsJumping = false;
        private static float jumpTime = 0f;
        private static float jumpDuration = 0.3f;
        private static bool jumpHadHit = false;


        private static bool jumpScored = false;
        private static int score = 0;
        private static int missed = 0;
        private static int missedJumps = 0;
        private static bool gameOver = false;


        private static IKeyboard keyboard;

        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string TextureUniformVariableName = "uTexture";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";


        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.WindowBorder = WindowBorder.Resizable;
            windowOptions.Title = "StepMania";
            windowOptions.PreferredDepthBufferBits = 24;
            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }


        private static void Window_Load()
        {

            var inputContext = window.CreateInput();
            keyboard = inputContext.Keyboards[0];

            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            // init OpenGL
            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);


            // create game objects
            SetUpObjects();


            LinkProgram();

            // init ImGui
            controller = new ImGuiController(Gl, window, inputContext);
            ImGui.GetIO().FontGlobalScale = 1.5f;

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }


        // Link shaders 
        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, ReadShader("VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, ReadShader("FragmentShader.frag"));
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        // read shaders
        private static string ReadShader(string shaderFileName)
        {
            string path = Path.Combine("Shaders", shaderFileName);
            if (!File.Exists(path))
                Console.WriteLine("Shader files not found: " + path);

            return File.ReadAllText(Path.Combine("Shaders", shaderFileName));
        }


        //keyboard controls
        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.A:
                    if (!fishIsJumping && fishZ > 0)
                    {
                        fishTargetZ = fishZ - 1;
                        fishTargetX = fishX;
                        StartJump();
                    }
                    break;
                case Key.D:
                    if (!fishIsJumping && fishZ < 2)
                    {
                        fishTargetZ = fishZ + 1;
                        fishTargetX = fishX;
                        StartJump();
                    }
                    break;
                case Key.S:
                    if (!fishIsJumping && fishX > 0)
                    {
                        fishTargetX = fishX - 1;
                        fishTargetZ = fishZ;
                        StartJump();
                    }
                    break;
                case Key.W:
                    if (!fishIsJumping && fishX < 2)
                    {
                        fishTargetX = fishX + 1;
                        fishTargetZ = fishZ;
                        StartJump();
                    }
                    break;
                case Key.F:
                    isFirstPersonView = !isFirstPersonView;
                    break;


            }
        }
        private static void StartJump()
        {
            fishIsJumping = true;
            jumpTime = 0f;
            jumpScored = false;
            jumpHadHit = false;
        }


        // ____________________________________________________________________________________________
        // UPDATE _____________________________________________________________________________________

        private static void Window_Update(double deltaTime)
        {
            // Skip update if game is over
            if (gameOver)
                return;

            cubeArrangementModel.AdvanceTime(deltaTime);

            UpdateKeyboard();

            UpdateArrows(deltaTime);

            UpdateFishJump(deltaTime);

        }

        private static void UpdateKeyboard()
        {
            if (keyboard.IsKeyPressed(Key.Left))
                cameraDescriptor.DecreaseZYAngle();

            if (keyboard.IsKeyPressed(Key.Right))
                cameraDescriptor.IncreaseZYAngle();

            if (keyboard.IsKeyPressed(Key.Up))
                cameraDescriptor.DecreaseDistance();

            if (keyboard.IsKeyPressed(Key.Down))
                cameraDescriptor.IncreaseDistance();

            if (keyboard.IsKeyPressed(Key.U))
                cameraDescriptor.IncreaseZXAngle();

            if (keyboard.IsKeyPressed(Key.L))
                cameraDescriptor.DecreaseZXAngle();
        }

        private static void UpdateArrows(double deltaTime)
        {
            // increase time since last arrow spawn
            timeSinceLastSpawn += deltaTime;

            // spawn new arrow if cooldown expired
            if (timeSinceLastSpawn >= spawnCooldown)
            {
                timeSinceLastSpawn = 0.0;
                SpawnRandomArrow();
            }

            float tolerance = 0.2f;

            // uupdate moving arrows and check if they reached the center
            for (int i = movingArrows.Count - 1; i >= 0; i--)
            {
                var arrow = movingArrows[i];
                arrow.Update((float)deltaTime);

                bool isAtCenter = MathF.Abs(arrow.Position.X) < tolerance &&
                                  MathF.Abs(arrow.Position.Z) < tolerance;

                // if at center, start shrinking and remove from moving list
                if (isAtCenter)
                {
                    shrinkingArrows.Add((arrow, 1.0f));
                    movingArrows.RemoveAt(i);
                }
            }

            // update shrinking arrows
            for (int i = shrinkingArrows.Count - 1; i >= 0; i--)
            {
                var (arrow, scale) = shrinkingArrows[i];
                scale -= shrinkSpeed * (float)deltaTime;

                // if fully shrunk, count as missed
                if (scale <= 0f)
                {
                    shrinkingArrows.RemoveAt(i);
                    missed++;
                    Console.WriteLine($"Missed arrows: {missed}");

                    // trigger game over if too many total misses
                    int totalMisses = missed + missedJumps;
                    if (totalMisses > 5)
                    {
                        gameOver = true;
                        Console.WriteLine("GAME OVER triggered due to too many missed arrows.");
                    }
                }
                else
                {
                    // update scale for ongoing shrink
                    shrinkingArrows[i] = (arrow, scale);
                }
            }
        }


        private static void SpawnRandomArrow()
        {
            // random direction
            var direction = (Direction)rng.Next(0, 4);
            Console.WriteLine("Spawned arrow direction: " + direction);

            // choose the starting pos based on direction
            Vector3D<float> startPos = direction switch
            {
                Direction.Up => new Vector3D<float>(0f, 0.11f, -2.5f), // red
                Direction.Down => new Vector3D<float>(0f, 0.11f, 2.5f), // yellow
                Direction.Left => new Vector3D<float>(2.5f, 0.11f, 0f), // green
                Direction.Right => new Vector3D<float>(-2.5f, 0.11f, 0f), // blue
                _ => new Vector3D<float>(0f, 0.11f, -2.5f)
            };

            // set color based on direction
            float[] color = direction switch
            {
                Direction.Up => new float[] { 1f, 0f, 0f, 1f }, // red
                Direction.Down => new float[] { 1f, 1f, 0f, 1f }, // yellow
                Direction.Left => new float[] { 0f, 1f, 0f, 1f }, //green
                Direction.Right => new float[] { 0f, 0f, 1f, 1f }, //blue
                _ => new float[] { 1f, 1f, 1f, 1f }
            };

            // create the moving arrow
            var arrowModel = GlArrow.CreateArrow(Gl, color);
            movingArrows.Add(new MovingArrow(arrowModel, startPos, direction));
        }

        private static void UpdateFishJump(double deltaTime)
        {
            if (!fishIsJumping) return;

            // increase jump timer
            jumpTime += (float)deltaTime;

            // calculate current fish position in world space
            float spacing = 1.27f;
            float jumpProgress = MathF.Min(jumpTime / jumpDuration, 1f);
            float currentX = fishX + (fishTargetX - fishX) * jumpProgress;
            float currentZ = fishZ + (fishTargetZ - fishZ) * jumpProgress;

            float fishWorldX = (currentX - 1) * spacing;
            float fishWorldZ = (currentZ - 1) * spacing;

            float hitboxSize = 0.12f;

            // check for collisions with moving arrows
            for (int i = movingArrows.Count - 1; i >= 0; i--)
            {
                var arrow = movingArrows[i];

                float dx = MathF.Abs(fishWorldX - arrow.Position.X);
                float dz = MathF.Abs(fishWorldZ - arrow.Position.Z);

                if (dx < hitboxSize && dz < hitboxSize)
                {
                    bool isCenter = (fishTargetX == 1 && fishTargetZ == 1);
                    Console.WriteLine(isCenter
                        ? "\nCollision at center tile\nNo score."
                        : "\nCollision outside center\nScore++");

                    Console.WriteLine($"Arrow position: ({arrow.Position.X:F2}, {arrow.Position.Z:F2})");
                    Console.WriteLine($"X: {dx:F2}, Z: {dz:F2}\n\n");

                    if (!isCenter)
                    {
                        // score if hit is not at center
                        score++;
                        jumpScored = true;
                        Console.WriteLine("New Score: " + score);
                    }

                    // mark that fish hit something
                    jumpHadHit = true;
                    movingArrows.RemoveAt(i);
                }
            }

            // end jump if time exceeded
            if (jumpTime >= jumpDuration)
            {
                fishIsJumping = false;
                fishX = fishTargetX;
                fishZ = fishTargetZ;
                Console.WriteLine($"Fish landed on tile [{fishX}, {fishZ}]");

                bool landedAtCenter = (fishTargetX == 1 && fishTargetZ == 1);

                // count as missed jump if no hit, no score, not center
                if (!jumpScored && !jumpHadHit && !landedAtCenter)
                {
                    missedJumps++;
                    Console.WriteLine($"Missed jumps: {missedJumps}");

                    int totalMisses = missed + missedJumps;
                    if (totalMisses > 5)
                    {
                        gameOver = true;
                        Console.WriteLine("GAME OVER triggered due to too many total misses.");
                    }
                }
            }
        }




        // ____________________________________________________________________________________________
        // RENDER _____________________________________________________________________________________

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();


            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();


            // RENDER OBJECTS
            DrawFish();
            PlatformRender();
            ArrowsRender();
            DrawSkyBox();

            //GUI
            controller.Update((float)deltaTime);
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(320, 300), ImGuiCond.Always);

            ImGui.Begin("Game Info");

            ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.1f, 1f), "HOW TO PLAY:");
            ImGui.TextWrapped("- Use W/A/S/D to jump.");
            ImGui.TextWrapped("- Hit arrows (not at center) to gain points.");
            ImGui.TextWrapped("- Avoid missing jumps.");
            ImGui.TextWrapped("- Game over after 5 total misses.");
            ImGui.Separator();


            if (gameOver)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1f, 0f, 0f, 1f), "GAME OVER!");
                ImGui.Text($"Final Score: {score}");
                if (ImGui.Button("Restart"))
                {
                    RestartGame();
                }
            }
            else
            {
                ImGui.Text($"Score: {score}");
                ImGui.Text($"Missed arrows: {missed}");
                ImGui.Text($"Missed jumps: {missedJumps}");
            }

            ImGui.End();
            controller.Render();
        }
        private static unsafe void PlatformRender()
        {
            int i = 0;
            float spacing = 1.27f;

            for (int row = -1
                ; row <= 1; row++)
            {
                for (int col = -1; col <= 1; col++)
                {
                    var translation = Matrix4X4.CreateTranslation(col * spacing, 0f, row * spacing);
                    SetModelMatrix(translation);
                    Gl.BindVertexArray(platformCubes[i].Vao);
                    Gl.DrawElements(GLEnum.Triangles, platformCubes[i].IndexArrayLength, GLEnum.UnsignedInt, null);
                    Gl.BindVertexArray(0);
                    i++;
                }
            }

        }

        private static unsafe void ArrowsRender()
        {

            var arrowTransforms = new[]
            {
                Matrix4X4.CreateScale<float>(0.9f) *
                Matrix4X4.CreateRotationX((float)Math.PI / -2) *
                Matrix4X4.CreateTranslation(0.0f, 0.11f, -0.90f),

                Matrix4X4.CreateScale<float>(0.9f) *
                Matrix4X4.CreateRotationX((float)Math.PI / -2) *
                Matrix4X4.CreateRotationY((float)Math.PI / -2) *
                Matrix4X4.CreateTranslation(0.90f, 0.11f, 0f),

                Matrix4X4.CreateScale<float>(0.9f) *
                Matrix4X4.CreateRotationX((float)Math.PI / -2) *
                Matrix4X4.CreateRotationY((float)Math.PI / 2) *
                Matrix4X4.CreateTranslation(-0.90f, 0.11f, 0f),

                Matrix4X4.CreateScale<float>(0.9f) *
                Matrix4X4.CreateRotationX((float)Math.PI / -2) *
                Matrix4X4.CreateRotationY((float)Math.PI) *
                Matrix4X4.CreateTranslation(0.0f, 0.11f, 0.90f)
            };

            for (int j = 0; j < 4; j++)
            {
                SetModelMatrix(arrowTransforms[j]);
                Gl.BindVertexArray(arrows[j].Vao);
                Gl.DrawElements(GLEnum.Triangles, arrows[j].IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
            }
            foreach (var mArrow in movingArrows)
            {
                SetModelMatrix(mArrow.GetTransformMatrix());
                Gl.BindVertexArray(mArrow.GlArrow.Vao);
                Gl.DrawElements(GLEnum.Triangles, mArrow.GlArrow.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
            }

            foreach (var shrinking in shrinkingArrows)
            {
                var scaleMatrix = Matrix4X4.CreateScale(shrinking.scale);
                var modelMatrix = scaleMatrix * shrinking.arrow.GetTransformMatrix();
                SetModelMatrix(modelMatrix);
                Gl.BindVertexArray(shrinking.arrow.GlArrow.Vao);
                Gl.DrawElements(GLEnum.Triangles, shrinking.arrow.GlArrow.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
            }
        }

        private static void RestartGame()
        {
            score = 0;
            missed = 0;
            missedJumps = 0;
            gameOver = false;
            fishX = 1;
            fishZ = 1;
            fishTargetX = 1;
            fishTargetZ = 1;
            fishIsJumping = false;
            jumpTime = 0f;
            movingArrows.Clear();
            timeSinceLastSpawn = 0.0;
        }

        private static unsafe void DrawSkyBox()
        {
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(400f);
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(skyBox.Vao);

            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");
            }
            // set texture 0
            Gl.Uniform1(textureLocation, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, skyBox.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, skyBox.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            CheckError();
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            CheckError();
        }



        private static unsafe void DrawFish()
        {
            // set material uniform to rubber
            float spacing = 1.27f;

            // calculate how far along the jump animation is (0 to 1)
            float jumpProgress = MathF.Min(jumpTime / jumpDuration, 1f);

            //det current fish position to coordinates
            float currentX = fishX;
            float currentZ = fishZ;
            float height = 0f;

            // jump
            if (fishIsJumping)
            {
                currentX = fishX + (fishTargetX - fishX) * jumpProgress;
                currentZ = fishZ + (fishTargetZ - fishZ) * jumpProgress;

                // the degree of the jump
                height = MathF.Sin(jumpProgress * MathF.PI) * 0.4f;
            }


            // correct position for fish
            var modelMatrix =
            Matrix4X4.CreateScale(0.11f) *
            Matrix4X4.CreateRotationX((float)Math.PI / -2f) *
            Matrix4X4.CreateTranslation(
            (currentX - 1) * spacing,
            0.70f + height,
            (currentZ - 1) * spacing);

            //upload matrix to shader
            SetModelMatrix(modelMatrix);

            // radw the fish object
            Gl.BindVertexArray(fish.Vao);
            Gl.DrawElements(GLEnum.Triangles, fish.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            // copy of the model matrix without translation components
            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));


            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }

            // a kiszamito tt modelmatrix felroltese a shaderbe
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {
            // PLATFORM

            for (int row = -1; row <= 1; row++)
            {
                for (int col = -1; col <= 1; col++)
                {
                    var cube = GlCube.CreateCubeWithFaceColors(Gl,
                       new float[] { 0.5f, 0.5f, 0.5f, 1f },
                       new float[] { 0.5f, 0.5f, 0.5f, 1f },
                       new float[] { 0.5f, 0.5f, 0.5f, 1f },
                       new float[] { 0.5f, 0.5f, 0.5f, 1f },
                       new float[] { 0.5f, 0.5f, 0.5f, 1f },
                       new float[] { 0.5f, 0.5f, 0.5f, 1f }


                    );
                    platformCubes.Add(cube);
                }
            }
            //---------------
            //ARROWS
            for (int i = 0; i < 4; i++)
            {
                arrows.Add(GlArrow.CreateArrow(Gl, new float[] { 1f, 0f, 0f, 1f })); // red
                arrows.Add(GlArrow.CreateArrow(Gl, new float[] { 0f, 1f, 0f, 1f })); // green
                arrows.Add(GlArrow.CreateArrow(Gl, new float[] { 0f, 0f, 1f, 1f })); // blue
                arrows.Add(GlArrow.CreateArrow(Gl, new float[] { 1f, 1f, 0f, 1f })); // yellow


            }
            //----------------------

            //SKYBOX
            skyBox = GlCube.CreateInteriorCube(Gl, "");

            //FISH
            fish = ObjResourceReader.CreateFishWithColor(Gl, new float[] { 1f, 0.5f, 0f, 1f }); // orange fish
        }


        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 1000);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            Matrix4X4<float> viewMatrix;

            if (isFirstPersonView)
            {
                float spacing = 2f;
                float jumpProgress = MathF.Min(jumpTime / jumpDuration, 1f);

                // current fish position and height
                float currentX = fishX;
                float currentZ = fishZ;
                float height = 2f;

                // fish position during jump
                if (fishIsJumping)
                {
                    currentX = fishX + (fishTargetX - fishX) * jumpProgress;
                    currentZ = fishZ + (fishTargetZ - fishZ) * jumpProgress;

                    // simulate jumping arc
                    height += MathF.Sin(jumpProgress * MathF.PI) * 0.4f;
                }

                // world position of the fish
                Vector3D<float> fishWorldPos = new Vector3D<float>(
                    (currentX - 1) * spacing,
                    height,
                    (currentZ - 1) * spacing
                );

                // set camera slightly above the fish
                Vector3D<float> cameraPos = fishWorldPos + new Vector3D<float>(0f, 5f, 0.0001f); 
                Vector3D<float> targetPos = fishWorldPos;

                // create view matrix from camera to fish
                viewMatrix = Matrix4X4.CreateLookAt(cameraPos, targetPos, Vector3D<float>.UnitX);

            }
            else
            {
                // default view matrix from camera descriptor (third-person)
                viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            }

            // upload view matrix to shader
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (location == -1)
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }
        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 1f, 1f, 1f);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 5f, 1f, 0f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }


        private static void Window_Closing()
        {
            controller?.Dispose();

            Environment.Exit(0);
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}