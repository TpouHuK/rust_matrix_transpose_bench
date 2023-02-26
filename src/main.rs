use rand::{rngs::SmallRng, Rng, SeedableRng};
use std::time::Instant;

fn main() {
    let sizes = vec![100, 1000, 10_000, 50_000];

    for n in sizes {
        let mut matrix = vec![vec![0; n]; n];

        let start_gen = Instant::now();
        let mut rng = SmallRng::from_entropy();
        for i in 0..n {
            for j in 0..n {
                matrix[i][j] = rng.gen::<i32>() % 11;
            }
        }
        let elapsed = start_gen.elapsed();
        println!("It took {elapsed:?} to generate matrix.");
        println!(
            "Measuring the time it takes to transpose a {n} by {n} matrix in the main thread..."
        );

        let sw = Instant::now();
        for i in 0..n {
            for j in 0..(n - 1 - i) {
                (matrix[i][j], matrix[n - 1 - j][n - 1 - i]) =
                    (matrix[n - 1 - j][n - 1 - i], matrix[i][j]);
            }
        }

        let elapsed = sw.elapsed();
        println!("Time taken: {elapsed:?} .");

        for thread_count in 2..=3 {
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

                    let mut phantom_matrix;
                    unsafe {
                        phantom_matrix = Vec::from_raw_parts(matrix.as_mut_ptr(), matrix.len(), matrix.capacity());
                    };

                    s.spawn(move || {
                        let matrix_ptr = &mut phantom_matrix;
                        thread_with_state(begin, count, matrix_ptr);
                        std::mem::forget(phantom_matrix);
                    });
                }
            });

            let elapsed = sw.elapsed();
            println!("Dividing into {thread_count} threads and executing took {elapsed:?}.");
        }
    }
}

fn thread_with_state(begin: usize, mut count: usize, matrix: &mut Vec<Vec<i32>>) {
    let n = matrix.len();
    let i_begin = begin / n;
    let j_begin = begin % n;

    for j in j_begin..(n - 1 - i_begin) {
        (matrix[i_begin][j], matrix[n - 1 - j][n - 1 - i_begin]) =
            (matrix[n - 1 - j][n - 1 - i_begin], matrix[i_begin][j]);
        count -= 1;
    }

    let mut i = i_begin + 1;

    while count > 0 {
        i += 1;

        for j in 0..(n - 1 - i) {
            if count == 0 { break }
            (matrix[i][j], matrix[n - 1 - j][n - 1 - i]) =
                (matrix[n - 1 - j][n - 1 - i], matrix[i][j]);
            count -= 1;
        }
    }
}
