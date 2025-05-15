using Silk.NET.OpenGL;

namespace Szeminarium1_24_02_17_2
{
    internal class GlArrow
    {
        public uint Vao { get; }
        public uint Vertices { get; }
        public uint Colors { get; }
        public uint Indices { get; }
        public uint IndexArrayLength { get; }

        private GL Gl;

        private GlArrow(uint vao, uint vertices, uint colors, uint indexArrayLength, GL gl)
        {
            this.Vao = vao;
            this.Vertices = vertices;
            this.Colors = colors;
            this.IndexArrayLength = indexArrayLength;
            this.Gl = gl;
        }

        public static unsafe GlArrow CreateArrow(GL Gl, float[] face1Color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float[] vertexArray = new float[]
            {
                -0.2f, 0.0f, 0.05f,  0f, 0f, 1f, //0
                 0.2f, 0.0f, 0.05f,  0f, 0f, 1f, //1
                 0.2f, 0.6f, 0.05f,  0f, 0f, 1f, //3
                -0.2f, 0.6f, 0.05f,  0f, 0f, 1f, //4

                -0.2f, 0.0f, -0.05f,  0f, 0f, -1f, //4
                 0.2f, 0.0f, -0.05f,  0f, 0f, -1f, //5
                 0.2f, 0.6f, -0.05f,  0f, 0f, -1f, //6
                -0.2f, 0.6f, -0.05f,  0f, 0f, -1f, //7

                -0.4f, 0.6f, 0.05f,  0f, 0f, 1f, //8
                 0.4f, 0.6f, 0.05f,  0f, 0f, 1f, //9
                 0.0f, 0.9f, 0.05f,  0f, 0f, 1f, //10
                  
                -0.4f, 0.6f, -0.05f,  0f, 0f, -1f, //11
                 0.4f, 0.6f, -0.05f,  0f, 0f, -1f, //12
                 0.0f, 0.9f, -0.05f,  0f, 0f, -1f, //13

            };

            List<float> colorsList = new List<float>();
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            for (int i = 0; i < 5; i++)
                colorsList.AddRange(face1Color);

            float[] colorArray = colorsList.ToArray();



            uint[] indexArray = new uint[]
            {
               0, 1, 2,
               0, 2, 3,

               4, 5, 6,
               4, 6, 7,

               0, 4, 3,
               3, 4, 7,

               2, 1, 5,
               5, 2, 6, 

               0, 1, 5, 
               0, 5, 4,

               3, 2, 6, 
               3, 6, 7,


               8, 9, 10,
               11, 12, 13,

               8, 12, 9,
               8, 12, 11,

               8, 13, 11,
               8, 13, 10,

               9, 13, 10,
               9, 13, 12
            };
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)indexArray.Length;

            return new GlArrow(vao, vertices, colors, indexArrayLength, Gl);
        }

        internal void ReleaseGlCube()
        {
            Gl.DeleteBuffer(Vertices);
            Gl.DeleteBuffer(Colors);
            Gl.DeleteBuffer(Indices);
            Gl.DeleteVertexArray(Vao);
        }
    }
}
