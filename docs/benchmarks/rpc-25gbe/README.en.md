# RTX 5090 + Tesla V100 over RPC on 25 GbE

[Русская версия](README.md)

> **Clarification:** additional PCIe, TCP and RDMA measurements for Qwen3.6-27B are available in a [separate supplement](../qwen-transport/README.en.md).

A practical GGUF inference benchmark for llama.cpp/LlamaStudio. It compares a local RTX 5090 run with a distributed run that places part of the model on a remote Tesla V100 through RPC over a dedicated 25 GbE Ethernet link.

> Measurements and article by the LlamaStudio author (`satspace-cpu`). This is a report about one practical configuration, not a universal GPU or NIC ranking.

## Configuration

| Node | Hardware and software |
|---|---|
| Local | NVIDIA GeForce RTX 5090 32 GB, ASRock X870E Taichi, AMD Ryzen 9 9850X3D, 64 GB RAM, Windows 11 |
| Remote RPC | NVIDIA Tesla V100 32 GB, Windows 11 |
| Network | Mellanox MCX4121A-ACUT on both nodes, direct 25 GbE link |
| Network settings | Jumbo Packet `9014`, `NlMtuBytes 9000` |
| Stack | llama.cpp / llama-server with ggml-rpc-server, managed by LlamaStudio |

PCIe P2P, GPUDirect and RDMA were not used. The results therefore characterize the regular Windows 11 + TCP/IP + RPC path, not the maximum theoretical capability of the Mellanox adapters.

## Method

- Every comparable profile was run twice; the table contains the average.
- The measured values are prompt-processing speed and generation speed in tokens per second.
- The local and RPC runs used the same prompt and test parameters.
- `78/22` and `65/35` are tensor-split / layer-distribution ratios between GPUs. They are neither GPU utilization nor network-throughput percentages.

## Results

| Profile | Local prompt | RPC prompt | Change | Local generation | RPC generation | Change |
|---|---:|---:|---:|---:|---:|---:|
| Muse-Glimmer-30B-UD-Q8_K_XL, MTP 5 (Muse4-100) | 2178.61 | 1331.74 | -38.9% | 87.68 | 43.71 | -50.2% |
| Muse-Glimmer-30B-UD-Q8_K_XL, MTP 5 (Muse5-70) | 2091.62 | 1331.74 | -36.3% | 65.67 | 43.71 | -33.4% |
| Muse-Glimmer-30B-UD-Q8_K_XL, MTP 3 | 2137.93 | 1347.14 | -37.0% | 82.43 | 49.51 | -39.9% |
| Qwen3.6-27B-UD-Q8_K_XL, F16, MTP 5 | 1749.50 | 1033.60 | -40.9% | 76.07 | 58.11 | -23.6% |
| ThinkingCap-Qwen3.6-27B-Q8_0-MTP, F16, MTP 5 | 1918.88 | 924.20 | -51.8% | 89.22 | 68.57 | -23.1% |

Across the five comparable profiles, prompt processing decreased by **41.0%** on average and generation speed decreased by **34.1%**.

![Prompt and generation dashboard](assets/rpc-dashboard.png)

![Local versus RPC comparison table](assets/comparison-table.png)

![All raw measurements](assets/all-measurements.png)

## Interpretation

Large-prompt processing sees the largest reduction. With tensor split, part of the compute and intermediate data must cross the network boundary, so even a direct 25 GbE link adds RPC latency and transport overhead.

Generation is less affected in this configuration, but it remains slower than a local run. Qwen 3.6 27B and ThinkingCap Qwen retained the best generation performance, at roughly a 23% reduction versus their local results.

This report does not claim that 25 GbE itself is limited to these figures. It measures the cost of distributing these particular models and this llama.cpp configuration over RPC. Results depend on the model, quantization, tensor split, context length, MTP, llama.cpp version and network latency.

## Source data

- [Formula-driven Excel report with the complete measurements](data/gpu_rpc_25gb_report.xlsx)
- Charts are available in [`assets`](assets/).

## Limitation

The source dataset does not record the remote node's CPU or RAM capacity. The article therefore documents network configuration and inference behavior, but does not attribute the measured difference to the GPU or network alone.
