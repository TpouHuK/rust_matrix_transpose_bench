using System.Diagnostics; 
using System; 
using System.Threading; 
 
var rand = new Random(); 
 
var sizes = new[] { 100, 1000, 10000, 20000 }; 
 
foreach (var n in sizes)  
{ 
    Console.WriteLine("Generating the matrix..."); 
    var matrix = new float[n, n]; 
    var originalMatrix = new float[n, n]; 
    for (var i = 0; i < n; i++)  
    { 
        for (var j = 0; j < n; j++) 
        { 
            matrix[i, j] = rand.Next(10); 
            originalMatrix[i, j] = matrix[i, j]; 
        } 
    } 
 
    Console.WriteLine("Measuring the time it takes to transpose "  
                          + $"a {n} by {n} matrix in the main thread..."); 
    var sw = new Stopwatch(); 
    sw.Start(); 
 
    for (var i = 0; i < n; i++) 
    { 
        for (var j = 0; j < n - 1 - i; j++) { 
            (matrix[i, j], matrix[n - 1 - j, n - 1 - i]) = (matrix[n - 1 - j, n - 1 - i], matrix[i, j]); 
        } 
    } 
 
    sw.Stop(); 
    Console.WriteLine($"Time taken: {sw.Elapsed.TotalMilliseconds} ms."); 
 
    for (var threadCount = 4; threadCount <= 128; threadCount *= 2) 
    { 
        // reset the matrix 
        for (var i = 0; i < n; i++) 
        { 
            for (var j = 0; j < n; j++) 
            { 
                matrix[i, j] = originalMatrix[i, j]; 
            } 
        } 
 
        sw.Restart(); 
 
        long totalCount = (long) n * (n - 1) / 2; 
        long count = totalCount / threadCount; 
        var threads = new Thread[threadCount]; 
        for (var i = 0; i < threadCount; i++) 
        { 
            var begin = i * count; 
            if (i == threadCount - 1) 
            { 
                count = totalCount - (threadCount - 1) * count; 
            } 
 
            var tws = new ThreadWithState(matrix, begin, count); 
            threads[i] = new Thread(tws.ThreadProc); 
            threads[i].Start(); 
        } 
 
        for (var i = 0; i < threadCount; i++)  
        { 
            threads[i].Join(); 
        } 
 
        sw.Stop(); 
        Console.WriteLine($"Dividing into {threadCount} threads and executing took " 
                          + $"{sw.Elapsed.TotalMilliseconds} ms."); 
         
        // check correctness 
        var incorrectCounter = 0; 
        for (var i = 0; i < n; i++) 
        { 
            for (var j = 0; j < n - 1 - i; j++) { 
                if (matrix[i, j] != originalMatrix[n - 1 - j, n - 1 - i]) 
                { 
                    incorrectCounter++; 
                } 
            } 
        } 
 
        if (incorrectCounter != 0) 
        { 
            Console.WriteLine($"{incorrectCounter} incorrect"); 
        } 
    } 
} 
 
public class ThreadWithState 
{ 
    private float[,] matrix; 
    private int n; 
    private long begin, count; 
 
    public ThreadWithState(float[,] matrix, long begin, long count) 
    { 
        this.matrix = matrix; 
        this.n = matrix.GetLength(0); 
        this.begin = begin; 
        this.count = count; 
    } 
 
    public void ThreadProc()  
    { 
        // find the beginning 
        int i = 0; 
        int j = 0; 
 
        for (i = 0; i < n; i++) 
        { 
            for (j = 0; j < n - 1 - i; j++) 
            { 
                if (begin == 0)  
                { 
                    break; 
                } 
 
                begin--; 
            } 
 
            if (begin == 0) 
            { 
                break; 
            } 
        } 
 
        // do the transposition 
        while (count > 0) 
        { 
            while (count > 0) 
            { 
                if (j >= n - 1 - i) 
                { 
                    j = 0; 
                    break; 
                } 
 
                (matrix[i, j], matrix[n - 1 - j, n - 1 - i])  
                    = (matrix[n - 1 - j, n - 1 - i], matrix[i, j]); 
 
                count--; 
                j++; 
            } 
 
            i++; 
        } 
    } 
}
