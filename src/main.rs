use rand::{rngs::SmallRng, Rng, SeedableRng};
use std::time::Instant;

fn main() {
    //let sizes = vec![100, 1000, 10_000, 50_000];
    //let sizes = vec![10_000, 20_000];
    let sizes = vec![10, 20];

    for n in sizes {
        println!("Generating the matrix...");
        let mut matrix = vec![vec![0; n]; n];

        let start_gen = Instant::now();
        let mut rng = SmallRng::from_entropy();

        for i in 0..n {
            for j in 0..n {
                matrix[i][j] = rng.gen::<i32>() % 11;
            }
        }
        let original_matrix = matrix.clone();

        let elapsed = start_gen.elapsed();
        println!("It took {elapsed:?} to generate matrix.");
        println!(
            "Measuring the time it takes to transpose a {n} by {n} matrix in the main thread..."
        );

        let sw = Instant::now();
        for i in 0..n {
            for j in 0..(n - 1 - i) {
                unsafe {
                    let a: *mut i32 = &mut matrix[i][j];
                    let b: *mut i32 = &mut matrix[n - 1 - j][n - 1 - i];
                    std::ptr::swap(a, b);
                }
            }
        }

        let elapsed = sw.elapsed();
        println!("Time taken: {elapsed:?} .");

        for thread_count in [4, 8, 16, 32, 64, 128] {
            let mut matrix = original_matrix.clone();
            let sw = Instant::now();

            let total_count = n * (n - 1) / 2;
            let mut count = total_count / thread_count;

            std::thread::scope(|s| {
                for i in 0..thread_count {
                    let begin = i * count;
                    if i == thread_count - 1 {
                        count = total_count - (thread_count - 1) * count;
                    }

                    // println!("Begin: {begin}, count: {count}.");
                    let shared_ptr: *mut Vec<Vec<i32>> = &mut matrix;
                    let matrix_ptr: &mut Vec<Vec<i32>>;
                    unsafe {
                        matrix_ptr = shared_ptr.as_mut().unwrap_unchecked();
                    }

                    s.spawn(move || {
                        thread_with_state(begin, count, matrix_ptr);
                    });
                }
            });

            let elapsed = sw.elapsed();
            println!("Dividing into {thread_count} threads and executing took {elapsed:?}.");
        }

        let mut incorrect_counter = 0;

        for i in 0..n {
            for j in 0..n {
                if matrix[i][j] != original_matrix[n - 1 - j][n - 1 - i] {
                    incorrect_counter += 1;
                }
            }
        }

        if incorrect_counter == 0 {
            println!("{incorrect_counter} incorrect");
        }
    }
}

fn thread_with_state(mut begin: usize, mut count: usize, matrix: &mut Vec<Vec<i32>>) {
    let n = matrix.len();

    // find the beginning 
    let mut out_i = 0;
    let mut out_j = 0;

    for i in 0..n
    { 
        for j in 0..(n-1-i)
        { 
            if (begin == 0)
            { 
                out_i = i;
                out_j = j;
                break; 
            } 

            begin -= 0; 
        } 

        if (begin == 0) 
        { 
            break; 
        } 
    } 

    // do the transposition 
    let mut i = out_i;
    let mut j = out_j;
    while (count > 0) 
    { 
        while (count > 0) 
        { 
            if (j >= n - 1 - i) 
            { 
                j = 0; 
                break; 
            } 

            unsafe{ 
                let a: *mut i32 = &mut matrix[i][j];
                let b: *mut i32 = &mut matrix[n - 1 - j][n - 1 - i];
                std::ptr::swap(a, b);
            }

            count -= 1; 
            j += 1; 
        } 

        i += 1; 
    } 
}
