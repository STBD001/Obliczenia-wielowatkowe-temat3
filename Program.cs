using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixMultiplication
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] matrixSizes = { 200, 400, 600, 800, 1000 };
            int[] threadCounts = { 1, 2, 4, 8 };
            int numTests = 3;

            foreach (int size in matrixSizes)
            {
                Console.WriteLine($"\nTestowanie dla macierzy {size}x{size}:");

                Matrix matrixA = new Matrix(size, size);
                Matrix matrixB = new Matrix(size, size);
                matrixA.FillRandom(1, 10);
                matrixB.FillRandom(1, 10);

                if (size <= 10)
                {
                    Console.WriteLine("\nMacierz A:");
                    matrixA.Display();
                    Console.WriteLine("\nMacierz B:");
                    matrixB.Display();
                }

                
                Console.WriteLine($"\nWyniki dla macierzy {size}x{size} (Parallel):");
                Console.WriteLine("Liczba wątków | Czas (ms) | Przyspieszenie");
                Console.WriteLine("-------------|-----------|-------------");

                long referenceTimeParallel = 0;

                foreach (int threadCount in threadCounts)
                {
                    long totalTime = 0;

                    for (int test = 0; test < numTests; test++)
                    {
                        Matrix result = new Matrix(matrixA.Rows, matrixB.Columns);

                        Stopwatch stopwatch = Stopwatch.StartNew();
                        result = Matrix.MultiplyParallel(matrixA, matrixB, threadCount);
                        stopwatch.Stop();

                        totalTime += stopwatch.ElapsedMilliseconds;

                        if (size <= 10 && test == 0 && threadCount == 1)
                        {
                            Console.WriteLine("\nWynikowa macierz (Parallel):");
                            result.Display();
                            Console.WriteLine();
                        }
                    }

                    long averageTime = totalTime / numTests;

                    if (threadCount == 1)
                    {
                        referenceTimeParallel = averageTime;
                    }

                    double speedup = (double)referenceTimeParallel / averageTime;

                    Console.WriteLine($"{threadCount,12} | {averageTime,9} | {speedup,11:F2}");
                }
                

                Console.WriteLine($"\nWyniki dla macierzy {size}x{size} (Thread):");
                Console.WriteLine("Liczba wątków | Czas (ms) | Przyspieszenie");
                Console.WriteLine("-------------|-----------|-------------");

                long referenceTimeThread = 0;

                foreach (int threadCount in threadCounts)
                {
                    long totalTime = 0;

                    for (int test = 0; test < numTests; test++)
                    {
                        Matrix result = new Matrix(matrixA.Rows, matrixB.Columns);

                        Stopwatch stopwatch = Stopwatch.StartNew();
                        result = Matrix.MultiplyThread(matrixA, matrixB, threadCount);
                        stopwatch.Stop();

                        totalTime += stopwatch.ElapsedMilliseconds;

                        if (size <= 10 && test == 0 && threadCount == 1)
                        {
                            Console.WriteLine("\nWynikowa macierz (Thread):");
                            result.Display();
                            Console.WriteLine();
                        }
                    }

                    long averageTime = totalTime / numTests;

                    if (threadCount == 1)
                    {
                        referenceTimeThread = averageTime;
                    }

                    double speedup = (double)referenceTimeThread / averageTime;

                    Console.WriteLine($"{threadCount,12} | {averageTime,9} | {speedup,11:F2}");
                }
            }

            Console.WriteLine("\nNaciśnij dowolny klawisz, aby zakończyć...");
            Console.ReadKey();
        }
    }

    class Matrix
    {
        private readonly double[,] data;
        private static readonly Random random = new Random();

        public int Rows { get; }
        public int Columns { get; }

        public Matrix(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            data = new double[rows, columns];
        }

        public double this[int row, int column]
        {
            get => data[row, column];
            set => data[row, column] = value;
        }

        public void FillRandom(int min, int max)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    data[i, j] = random.Next(min, max);
                }
            }
        }

        public void Display()
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    Console.Write($"{data[i, j],4} ");
                }
                Console.WriteLine();
            }
        }

        public static Matrix MultiplyParallel(Matrix a, Matrix b, int numThreads)
        {
            if (a.Columns != b.Rows)
                throw new ArgumentException("Liczba kolumn pierwszej macierzy musi być równa liczbie wierszy drugiej macierzy");

            Matrix result = new Matrix(a.Rows, b.Columns);

            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = numThreads };

            Parallel.For(0, a.Rows, options, i =>
            {
                for (int j = 0; j < b.Columns; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < a.Columns; k++)
                    {
                        sum += a[i, k] * b[k, j];
                    }
                    result[i, j] = sum;
                }
            });

            return result;
        }

        public static Matrix MultiplyThread(Matrix a, Matrix b, int numThreads)
        {
            if (a.Columns != b.Rows)
                throw new ArgumentException("Liczba kolumn pierwszej macierzy musi być równa liczbie wierszy drugiej macierzy");

            Matrix result = new Matrix(a.Rows, b.Columns);
            Thread[] threads = new Thread[numThreads];

            int rowsPerThread = a.Rows / numThreads;
            int remainingRows = a.Rows % numThreads;

            int startRow = 0;

            for (int t = 0; t < numThreads; t++)
            {
                int threadRows = rowsPerThread + (t < remainingRows ? 1 : 0);
                int endRow = startRow + threadRows;

                int localStartRow = startRow;
                int localEndRow = endRow;

                threads[t] = new Thread(() =>
                {
                    for (int i = localStartRow; i < localEndRow; i++)
                    {
                        for (int j = 0; j < b.Columns; j++)
                        {
                            double sum = 0;
                            for (int k = 0; k < a.Columns; k++)
                            {
                                sum += a[i, k] * b[k, j];
                            }
                            result[i, j] = sum;
                        }
                    }
                });

                startRow = endRow;
            }

            foreach (Thread thread in threads)
            {
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            return result;
        }
    }
}