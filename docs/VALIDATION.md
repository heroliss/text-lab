# 1.0.0 validation

The final executable was exercised through a normal visible Windows session with native input and screen capture. Screenshots are real application output, not a mock-up or DrawToBitmap export.

## Core checks

62 passed, 0 failed. Coverage includes segmentation/offsets, custom vocabulary, frequency counts, Unicode chunk boundaries, fixed-phrase review, stale correction rejection, both sides of the context window, text/binary word2vec, corrupt and oversized model lines, cancellation checkpoints, strict UTF-8/GB18030/BOM handling, KMP read-buffer boundaries, mixed newlines, randomized matching against a reference, bounded long-line context, partial results, and isolated batch output.

## Native acceptance

- Self-contained Windows x64 executable starts without a separate .NET install.
- Dark text remains legible during asynchronous work; only inputs are locked and the header cancel action stays available.
- Chinese labels and content render without missing glyph boxes.
- The bundled public sample segments into 71 tokens / 49 distinct lexical words / 126 UTF-16 characters.
- Five fixed-phrase suggestions are shown with individual explanations. Applying the first changes only `迫不急待` to `迫不及待`; four suggestions remain.
- The local open-file dialog works from the STA UI thread.
- A clearly labelled, independently authored 8-word / 3-dimension teaching vector file imports successfully. It is not a trained model.
- The original user-owned local `vectors.bin` also imports successfully: 564,778 words × 100 dimensions, 231,342,702 source bytes. It was opened read-only in place and never copied into this repository or the release.
- On the validation machine, original model load was 4,032 ms. A two-word query returned 15 nearest neighbours in 184 ms. A local context pass returned 30 references in 69 ms. These are individual observations, not portable performance promises.
- A post-load process sample showed 608.2 MiB working set / 471.3 MiB private memory. These are point-in-time measurements, not peak-memory bounds.
- Mixed UTF-8 / GB18030 synthetic files produce 6 matching lines across 3 files, without duplicate decoding results. Selecting the GB18030 result updates the matching filename, line and context.
- The acceptance process and all its windows were closed after capture.

Native cancellation was not stress-tested through repeated artificial GUI runs. The core cancellation and partial-result paths are covered by deterministic checks. The attempts to resize the native window did not establish a useful size change, so no minimum-window acceptance claim is made.

## Distribution boundaries

No historical database, private corpus, credential, old executable or unknown-provenance vector model is included. The only text used for native examples was written for this project. Jieba's upstream MIT dictionary and HMM resources are embedded in the segmentation dependency and are documented in the third-party notices.

Eleven historical business source files are byte-preserved. `python scripts/verify-legacy.py` checks both checkout bytes and Git blob SHA-256 against their original manifest; line-ending conversion is disabled for the archives and license notices.
