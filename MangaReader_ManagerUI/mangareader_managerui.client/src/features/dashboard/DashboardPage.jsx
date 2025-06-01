import { Box, Grid, Paper, Typography } from '@mui/material'
import React from 'react'

function DashboardPage() {
  return (
    <Box className="dashboard-page">
      <Typography variant="h4" component="h1" className="dashboard-header">
        Chào mừng đến với Bảng điều khiển quản lý Manga
      </Typography>

      <Grid container spacing={4} className="dashboard-stats-grid" columns={{ xs: 4, sm: 6, md: 12 }}>
        <Grid item xs={4} sm={3} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Manga</Typography>
            <Typography variant="h4" color="primary">
              123
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={4} sm={3} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Tác giả</Typography>
            <Typography variant="h4" color="primary">
              45
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={4} sm={3} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Tổng số Tags</Typography>
            <Typography variant="h4" color="primary">
              67
            </Typography>
          </Paper>
        </Grid>
        <Grid item xs={4} sm={3} md={3}>
          <Paper className="stat-card">
            <Typography variant="h5">Chapter đã tải lên</Typography>
            <Typography variant="h4" color="primary">
              890
            </Typography>
          </Paper>
        </Grid>
      </Grid>

      <Paper sx={{ p: 3, boxShadow: 3, borderRadius: 2 }}>
        <Typography variant="h5" component="h2" gutterBottom>
          Thống kê nhanh
        </Typography>
        <Typography variant="body1">
          Đây là nơi hiển thị các biểu đồ và số liệu thống kê quan trọng về dữ liệu manga của bạn.
          Trong tương lai, bạn có thể tích hợp các biểu đồ từ thư viện như Chart.js hoặc Recharts
          để hiển thị các xu hướng hoặc thông tin tổng quan.
        </Typography>
      </Paper>
    </Box>
  )
}

export default DashboardPage 