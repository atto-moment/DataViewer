using System;

namespace DataViewer
{
    public struct MatrixOperation
    {
        public static double[][] Sum(double[][] A, double[][] B)
        {
            double[][] C = new double[A.Length][];
            if (A.Length == B.Length && A[0].Length == B[0].Length)
            {
                for (int i = 0; i < A.Length; i++)
                {
                    C[i] = new double[A[0].Length];
                    for (int j = 0; j < A[0].Length; j++)
                    {

                        C[i][j] = A[i][j] + B[i][j];
                    }
                }
                return C;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static double[] Sum(double[] A, double[] B)
        {
            double[] C = new double[A.Length];
            if (A.Length == B.Length)
            {
                for (int i = 0; i < A.Length; i++)
                {
                    C[i] = A[i] + B[i];
                }
                return C;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public static double[][] Product(double[][] A, double[][] B)
        {
            double[][] C = new double[A.Length][];
            if (A[0].Length == B.Length)
            {
                for (int i = 0; i < A.Length; i++)
                {
                    C[i] = new double[B[0].Length];
                    for (int j = 0; j < B[0].Length; j++)
                    {
                        C[i][j] = 0;
                        for (int k = 0; k < A[0].Length; k++)
                        {
                            C[i][j] += A[i][k] * B[k][j];
                        }
                    }
                }
                return C;
            }
            else 
            {  
                throw new ArgumentException();
            }
        }

        public static double[][] Transpose(double[][] A)
        {
            double[][] AT = new double[A[0].Length][];
            for (int i = 0; i < A[0].Length; i++)
            {
                AT[i] = new double[A.Length];
                for (int j = 0; j < A.Length; j++)
                {
                    AT[i][j] = A[j][i];
                }
            }
            return AT;
        }

        public static double[][] Rotate(double[] A) {
            double angleX = A[0] * (Math.PI / 180.0);
            double angleY = A[1] * (Math.PI / 180.0);
            double angleZ = A[2] * (Math.PI / 180.0);

            double[][] rotationX = [[1.0, 0.0, 0.0], [0.0, Math.Cos(angleX), -Math.Sin(angleX)], [0.0, Math.Sin(angleX), Math.Cos(angleX)]];
            double[][] rotationY = [[Math.Cos(angleY), 0.0, Math.Sin(angleY)], [0.0, 1.0, 0.0], [-Math.Sin(angleY), 0.0, Math.Cos(angleY)]];
            double[][] rotationZ = [[Math.Cos(angleZ), -Math.Sin(angleZ), 0.0], [Math.Sin(angleZ), Math.Cos(angleZ), 0.0], [0.0, 0.0, 1.0]];

            return Product(rotationZ, Product(rotationX, rotationY));
        }
    }
}
