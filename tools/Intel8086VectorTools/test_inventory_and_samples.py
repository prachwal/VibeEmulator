import gzip
import tempfile
import unittest
from pathlib import Path

from inventory_and_samples import count_source_records


class CountSourceRecordsTests(unittest.TestCase):
    def test_counts_key_split_across_read_buffer_boundary(self):
        chunk_size = 1 << 20
        needle = b'"test_num"'
        content = b" " * (chunk_size - 4) + needle + b"\n"

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "boundary.json.gz"
            with gzip.open(path, "wb") as stream:
                stream.write(content)

            count, uncompressed_bytes = count_source_records(path)

        self.assertEqual(1, count)
        self.assertEqual(len(content), uncompressed_bytes)


if __name__ == "__main__":
    unittest.main()
