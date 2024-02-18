CD Project README
Project Description
This Mixed Reality application leverages the Canny Algorithm enhanced with a Sobel Kernel for defect detection. It's engineered to identify and visualize defects within a Mixed Reality environment, 
using edge detection methods for analysis. This video shows how it works:


https://github.com/KaMa85/CD/assets/82784239/94788eb4-0266-40ee-ba48-7ef5964be40d



Development Stage
The current iteration of this application is not the final version. It has two primary limitations:
The Canny algorithm's parameters have not been normalized, leading to potential inconsistencies in edge detection accuracy.
An automatic Region of Interest (ROI) algorithm is not implemented, increasing manual input requirements, processing time, and overall operational cost.


Summary
The core functionality involves capturing images, converting them to grayscale, applying a Sobel Kernel to ascertain gradients, and then using these gradients within a 
Canny-based framework to detect edges indicative of defects. Due to the application's developmental nature, it currently does not feature parameter normalization or 
automatic ROI detection, impacting its efficiency and ease of use. Future versions will aim to refine these aspects for improved performance and user experience.
