using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;

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
        private static double spawnCooldown = 3.0;
        private static double timeSinceLastSpawn = 0.0;

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


        private static int score = 0;
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
            //windowOptions.WindowState = WindowState.Maximized;
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

            SetUpObjects();
            LinkProgram();

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
                //case Key.Left:
                //    cameraDescriptor.DecreaseZYAngle();
                //    break;
                //    ;
                //case Key.Right:
                //    cameraDescriptor.IncreaseZYAngle();
                //    break;
                //case Key.Down:
                //    cameraDescriptor.IncreaseDistance();
                //    break;
                //case Key.Up:
                //    cameraDescriptor.DecreaseDistance();
                //    break;
                //case Key.U:
                //    cameraDescriptor.IncreaseZXAngle();
                //    break;
                //case Key.L:
                //    cameraDescriptor.DecreaseZXAngle();
                //    break;
                //case Key.Space:
                //    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                //    break;
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
        }


        private static void Window_Update(double deltaTime)
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


            // Skip update if game is over
            if (gameOver)
                return;

            cubeArrangementModel.AdvanceTime(deltaTime);

            // Spawn new arrow if cooldown expired
            timeSinceLastSpawn += deltaTime;
            if (timeSinceLastSpawn >= spawnCooldown)
            {
                timeSinceLastSpawn = 0.0;
                SpawnRandomArrow();
            }

            // Update and remove arrows that left the screen
            for (int i = movingArrows.Count - 1; i >= 0; i--)
            {
                var arrow = movingArrows[i];
                arrow.Update((float)deltaTime);

                // Remove arrow if it moved out of bounds
                if (arrow.Direction == Direction.Up && arrow.Position.Z > 1.5f ||
                    arrow.Direction == Direction.Down && arrow.Position.Z < -1.5f ||
                    arrow.Direction == Direction.Left && arrow.Position.X < -1.5f ||
                    arrow.Direction == Direction.Right && arrow.Position.X > 1.5f)
                {
                    movingArrows.RemoveAt(i);
                }
            }

            // Handle fish jump animation and logic
            if (fishIsJumping)
            {
                jumpTime += (float)deltaTime;

                // If jump is complete
                if (jumpTime >= jumpDuration)
                {
                    jumpTime = 0f;
                    fishX = fishTargetX;
                    fishZ = fishTargetZ;
                    fishIsJumping = false;

                    // Only check for arrows if not landing in the center
                    if (!(fishX == 1 && fishZ == 1))
                    {
                        float spacing = 1.27f;
                        float expectedX = (fishX - 1) * spacing;
                        float expectedZ = (fishZ - 1) * spacing;

                        bool matched = false;

                        // Check if an arrow is close to the landing position
                        for (int i = movingArrows.Count - 1; i >= 0; i--)
                        {
                            var arrow = movingArrows[i];
                            if (MathF.Abs(arrow.Position.X - expectedX) < 0.5f &&
                                MathF.Abs(arrow.Position.Z - expectedZ) < 0.5f)
                            {
                                matched = true;
                                movingArrows.RemoveAt(i);
                                score++;
                                break;
                            }
                        }

                        // Game over if no arrow matched
                        if (!matched)
                        {
                            gameOver = true;
                        }
                    }
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
                Direction.Up => new Vector3D<float>(0f, 0.11f, -2.5f),
                Direction.Down => new Vector3D<float>(0f, 0.11f, 2.5f),
                Direction.Left => new Vector3D<float>(2.5f, 0.11f, 0f),
                Direction.Right => new Vector3D<float>(-2.5f, 0.11f, 0f),
                _ => new Vector3D<float>(0f, 0.11f, -2.5f)
            };

            // set color based on direction
            float[] color = direction switch
            {
                Direction.Up => new float[] { 1f, 0f, 0f, 1f },     
                Direction.Down => new float[] { 1f, 1f, 0f, 1f },   
                Direction.Left => new float[] { 0f, 1f, 0f, 1f },   
                Direction.Right => new float[] { 0f, 0f, 1f, 1f },  
                _ => new float[] { 1f, 1f, 1f, 1f }
            };

            // create the moving arrow
            var arrowModel = GlArrow.CreateArrow(Gl, color);
            movingArrows.Add(new MovingArrow(arrowModel, startPos, direction));
        }




        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");


            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();


            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();


            DrawPulsingFish();


            //// PLATFORM
            int i = 0;
            float spacing = 1.27f;

            for (int row = -1; row <= 1; row++)
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
            //---------------------
            ///ARROWS

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
            //---------------------

            DrawSkyBox();

            controller.Update((float)deltaTime);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.Always);

            ImGui.Begin("Game Info");

            if (gameOver)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1f, 0f, 0f, 1f), "GAME OVER!");
                ImGui.Text($"Final Score: {score}");
            }
            else
            {
                ImGui.Text($"Score: {score}");
            }

            ImGui.End();
            controller.Render();



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



        private static unsafe void DrawPulsingFish()
        {
            // set material uniform to rubber
            float spacing = 1.27f;
            float jumpProgress = MathF.Min(jumpTime / jumpDuration, 1f);

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


            var modelMatrix =
            Matrix4X4.CreateScale(0.11f) *
            Matrix4X4.CreateRotationX((float)Math.PI / -2f) *
            Matrix4X4.CreateTranslation(
            (currentX - 1) * spacing,
            0.70f + height,
            (currentZ - 1) * spacing);

            SetModelMatrix(modelMatrix);

            Gl.BindVertexArray(fish.Vao);
            Gl.DrawElements(GLEnum.Triangles, fish.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            //var modelMatrixForTable = Matrix4X4.CreateScale(0.02f, 0.02f, 0.02f);
            //SetModelMatrix(modelMatrixForTable);
            //Gl.BindVertexArray(table.Vao);
            //Gl.DrawElements(GLEnum.Triangles, table.IndexArrayLength, GLEnum.UnsignedInt, null);
            //Gl.BindVertexArray(0);
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
                arrows.Add(GlArrow.CreateArrow(Gl, new float[] { 1f, 0f, 0f, 1f })); // piros
                arrows.Add(GlArrow.CreateArrow(Gl, new float[] { 0f, 1f, 0f, 1f })); // zöld
                arrows.Add(GlArrow.CreateArrow(Gl, new float[] { 0f, 0f, 1f, 1f })); // kék
                arrows.Add(GlArrow.CreateArrow(Gl, new float[] { 1f, 1f, 0f, 1f })); // sárga


            }
            //----------------------

            skyBox = GlCube.CreateInteriorCube(Gl, "");
            fish = ObjResourceReader.CreateTeapotWithColor(Gl, new float[] { 1f, 0.5f, 0f, 1f }); // narancssárga hal
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

                float currentX = fishX;
                float currentZ = fishZ;
                float height = 2f;

                if (fishIsJumping)
                {
                    currentX = fishX + (fishTargetX - fishX) * jumpProgress;
                    currentZ = fishZ + (fishTargetZ - fishZ) * jumpProgress;
                    height += MathF.Sin(jumpProgress * MathF.PI) * 0.4f;
                }

                Vector3D<float> fishWorldPos = new Vector3D<float>(
                    (currentX - 1) * spacing,
                    height,
                    (currentZ - 1) * spacing
                );

                Vector3D<float> cameraPos = fishWorldPos + new Vector3D<float>(0f, 10f, 0.0001f); 
                Vector3D<float> targetPos = fishWorldPos;

                viewMatrix = Matrix4X4.CreateLookAt(cameraPos, targetPos, Vector3D<float>.UnitX);

            }
            else
            {
                viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            }

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
            //Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
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
            //foreach (var cube in platformCubes)
            //    cube.ReleaseGlCube();
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