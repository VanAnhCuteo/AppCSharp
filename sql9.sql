-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: localhost    Database: foodapp
-- ------------------------------------------------------
-- Server version	8.0.45

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `activity_logs`
--

DROP TABLE IF EXISTS `activity_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `activity_logs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `action_type` varchar(100) NOT NULL,
  `target_name` varchar(255) DEFAULT NULL,
  `details` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_log_user` (`user_id`),
  CONSTRAINT `fk_log_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `activity_logs`
--

LOCK TABLES `activity_logs` WRITE;
/*!40000 ALTER TABLE `activity_logs` DISABLE KEYS */;
INSERT INTO `activity_logs` VALUES (1,1,'Xóa quán ăn','Ốc Bùi Viện','Đã xóa Ốc Bùi Viện khỏi hệ thống','2026-03-25 22:50:39'),(2,1,'Xóa quán ăn','Bánh tráng nướng Bùi Viện','Đã xóa Bánh tráng nướng Bùi Viện khỏi hệ thống','2026-03-25 23:48:11'),(3,1,'Thêm quán ăn mới','bé iu','Đã tạo mới bé iu','2026-03-27 13:03:54'),(4,1,'Thêm danh mục','beaa','Đã tạo danh mục beaa','2026-03-29 15:25:44'),(5,1,'Xóa danh mục','beaa','Đã xóa danh mục beaa','2026-03-29 15:26:01'),(6,2,'Thêm ảnh mới','N/A','Đã tải lên ảnh WIN_20260208_15_56_11_Pro.jpg cho N/A','2026-03-30 22:41:52'),(7,1,'Xóa ảnh','N/A','Đã xóa một ảnh của N/A','2026-03-30 22:45:08'),(8,2,'Yêu cầu thay đổi','Hải sản 5KU','Đã gửi yêu cầu cập nhật thông tin cho Hải sản 5KU','2026-03-31 08:53:59'),(9,2,'Yêu cầu thay đổi','Hải sản Tươi Sống 68','Đã gửi yêu cầu cập nhật thông tin cho Hải sản Tươi Sống 68','2026-03-31 09:12:14'),(10,NULL,'Từ chối thay đổi','Hải sản Tươi Sống 68','Admin đã từ chối thay đổi cho Hải sản Tươi Sống 68','2026-03-31 09:12:54'),(11,NULL,'Từ chối thay đổi','Hải sản 5KU','Admin đã từ chối thay đổi cho Hải sản 5KU','2026-03-31 09:12:56'),(12,2,'Thêm quán ăn mới','Hải sản 5KU','Đã tạo mới Hải sản 5KU','2026-03-31 22:11:29'),(13,2,'Cập nhật ảnh','Hải sản 5KU','Đã sửa thông tin ảnh của Hải sản 5KU','2026-04-01 00:41:09'),(14,2,'Yêu cầu thay đổi','Hải sản 5KU','Đã gửi yêu cầu cập nhật thông tin cho Hải sản 5KU','2026-04-01 00:41:41');
/*!40000 ALTER TABLE `activity_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `categories`
--

DROP TABLE IF EXISTS `categories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `categories` (
  `category_id` int NOT NULL AUTO_INCREMENT,
  `category_name` varchar(100) NOT NULL,
  `description` text,
  PRIMARY KEY (`category_id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `categories`
--

LOCK TABLES `categories` WRITE;
/*!40000 ALTER TABLE `categories` DISABLE KEYS */;
INSERT INTO `categories` VALUES (1,'Hải sản','Các quán hải sản'),(2,'Ăn vặt','Các món ăn vặt'),(3,'Đồ nướng','BBQ và đồ nướng');
/*!40000 ALTER TABLE `categories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_guides`
--

DROP TABLE IF EXISTS `poi_guides`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_guides` (
  `guide_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int DEFAULT NULL,
  `title` varchar(200) DEFAULT NULL,
  `description` text,
  `language` varchar(20) DEFAULT NULL,
  PRIMARY KEY (`guide_id`),
  KEY `poi_guides_ibfk_1` (`poi_id`),
  CONSTRAINT `poi_guides_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_guides`
--

LOCK TABLES `poi_guides` WRITE;
/*!40000 ALTER TABLE `poi_guides` DISABLE KEYS */;
INSERT INTO `poi_guides` VALUES (17,9,'lịch sử quán','quán gia truyền, nước sốt đậm đà, phục vụ tận tình.\n','vi'),(18,9,'bar history','heirloom restaurant, rich sauce, dedicated service.','en'),(19,9,'酒吧历史记录','传家宝餐厅，丰富的酱汁，专属服务。','zh'),(20,3,'lịch sử quán','là một trong các quán nổi tiếng trên thế giới với nhiều món ăn độc đáo, ngon lắm.\n','vi'),(21,3,'bar history','is one of the famous restaurants in the world with many unique and delicious dishes.','en'),(22,3,'酒吧历史记录','是世界上著名的餐厅之一，有许多独特而美味的菜肴。','zh'),(23,4,'vân anh','vân anh vân anh vân anh vân anh vân anh','vi'),(24,4,'van Anh','so on so on so on so on so on so on so on so on','en'),(25,4,'van Anh','等等，等等，等等，等等，等等，等等','zh'),(29,6,'lịch sử quán','quán có từ năm 1999 với hương vi quê nhà.\n','vi'),(30,6,'bar history','the restaurant has been around since 1999 with the smell of home.','en'),(31,6,'酒吧历史记录','这家餐厅自1999年以来一直存在，充满了家的气息。','zh'),(32,7,'lịch sử quán','quán ăn thu hút nhiều người lớn,  trẻ nhỏ đến ăn uống\n','vi'),(33,7,'bar history','the restaurant attracts many adults and children to eat and drink','en'),(34,7,'酒吧历史记录','餐厅吸引了许多成人和儿童的饮食','zh');
/*!40000 ALTER TABLE `poi_guides` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_images`
--

DROP TABLE IF EXISTS `poi_images`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_images` (
  `image_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int DEFAULT NULL,
  `image_url` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`image_id`),
  KEY `poi_id` (`poi_id`),
  CONSTRAINT `poi_images_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`)
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_images`
--

LOCK TABLES `poi_images` WRITE;
/*!40000 ALTER TABLE `poi_images` DISABLE KEYS */;
INSERT INTO `poi_images` VALUES (1,NULL,'/images/oc1.jpg'),(2,NULL,'/images/banhtrang.jpg'),(3,NULL,'/images/oc2.jpg'),(4,NULL,'/images/oc3.jpg'),(5,NULL,'/images/oc4.jpg'),(6,NULL,'/images/banhtrang2.jpg'),(7,NULL,'/images/banhtrang3.jpg'),(8,NULL,'/images/banhtrang4.jpg'),(9,3,'/images/haisan5ku_1.jpg'),(10,3,'/images/haisan5ku_2.jpg'),(11,3,'/images/haisan5ku_3.jpg'),(12,3,'/images/haisan5ku_4.jpg'),(13,4,'/images/xiennuong_1.jpg'),(14,4,'/images/xiennuong_2.jpg'),(15,4,'/images/xiennuong_3.jpg'),(16,4,'/images/xiennuong_4.jpg'),(17,5,'/images/bbq_1.jpg'),(18,5,'/images/bbq_2.jpg'),(19,5,'/images/bbq_3.jpg'),(20,5,'/images/bbq_4.jpg'),(21,6,'/images/haisan68_1.jpg'),(22,6,'/images/haisan68_2.jpg'),(23,6,'/images/haisan68_3.jpg'),(24,6,'/images/haisan68_4.jpg'),(25,7,'/images/trasua_1.jpg'),(26,7,'/images/trasua_2.jpg'),(27,7,'/images/trasua_3.jpg'),(28,7,'/images/trasua_4.jpg');
/*!40000 ALTER TABLE `poi_images` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_pending_changes`
--

DROP TABLE IF EXISTS `poi_pending_changes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_pending_changes` (
  `change_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int NOT NULL,
  `category_id` int DEFAULT NULL,
  `name` varchar(200) DEFAULT NULL,
  `address` varchar(255) DEFAULT NULL,
  `latitude` decimal(10,7) DEFAULT NULL,
  `longitude` decimal(10,7) DEFAULT NULL,
  `open_time` varchar(50) DEFAULT NULL,
  `range_meters` int DEFAULT '50',
  `user_id` int DEFAULT NULL,
  `status` enum('pending','approved','rejected') DEFAULT 'pending',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`change_id`),
  KEY `fk_pending_poi` (`poi_id`),
  CONSTRAINT `fk_pending_poi` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_pending_changes`
--

LOCK TABLES `poi_pending_changes` WRITE;
/*!40000 ALTER TABLE `poi_pending_changes` DISABLE KEYS */;
INSERT INTO `poi_pending_changes` VALUES (1,3,1,'Hải sản 5KU','Bùi Viện Q1',10.7665796,106.6916712,'17:00 - 02:00',50,2,'rejected','2026-03-31 01:53:59'),(2,6,1,'Hải sản Tươi Sống 68','Bùi Viện Q1',10.7673543,106.6931453,'17:00 - 02:00',50,2,'rejected','2026-03-31 02:12:14'),(3,3,1,'Hải sản 5KU','Bùi Viện Q1',10.7671645,106.6930814,'17:00 - 02:00',50,2,'pending','2026-03-31 17:41:41');
/*!40000 ALTER TABLE `poi_pending_changes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `poi_visits`
--

DROP TABLE IF EXISTS `poi_visits`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `poi_visits` (
  `visit_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  `visit_time` datetime DEFAULT NULL,
  `poi_name` varchar(200) DEFAULT NULL,
  `poi_address` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`visit_id`),
  KEY `user_id` (`user_id`),
  KEY `poi_visits_ibfk_1` (`poi_id`),
  CONSTRAINT `poi_visits_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE SET NULL,
  CONSTRAINT `poi_visits_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_visits`
--

LOCK TABLES `poi_visits` WRITE;
/*!40000 ALTER TABLE `poi_visits` DISABLE KEYS */;
/*!40000 ALTER TABLE `poi_visits` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pois`
--

DROP TABLE IF EXISTS `pois`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pois` (
  `poi_id` int NOT NULL AUTO_INCREMENT,
  `category_id` int DEFAULT NULL,
  `name` varchar(200) NOT NULL,
  `address` varchar(255) DEFAULT NULL,
  `latitude` decimal(10,7) DEFAULT NULL,
  `longitude` decimal(10,7) DEFAULT NULL,
  `open_time` varchar(50) DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `user_id` int DEFAULT NULL,
  `range_meters` int DEFAULT '50',
  PRIMARY KEY (`poi_id`),
  KEY `category_id` (`category_id`),
  KEY `fk_pois_user` (`user_id`),
  CONSTRAINT `fk_pois_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`),
  CONSTRAINT `pois_ibfk_1` FOREIGN KEY (`category_id`) REFERENCES `categories` (`category_id`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pois`
--

LOCK TABLES `pois` WRITE;
/*!40000 ALTER TABLE `pois` DISABLE KEYS */;
INSERT INTO `pois` VALUES (3,1,'Hải sản 5KU','Bùi Viện Q1',10.7671645,106.6930814,'17:00 - 02:00','2026-03-20 04:58:01',2,50),(4,2,'Xiên nướng Bùi Viện','Bùi Viện Q1',10.7662906,106.6911645,'16:00 - 01:00','2026-03-20 04:58:01',3,50),(5,3,'BBQ Street','Bùi Viện Q1',10.7662906,106.6911645,'18:00 - 02:00','2026-03-20 04:58:01',4,50),(6,1,'Hải sản Tươi Sống 68','Bùi Viện Q1',10.7673543,106.6931453,'17:00 - 02:00','2026-03-21 15:13:56',2,50),(7,2,'Trà sữa phố Tây','Bùi Viện Q1',10.7656959,106.6904086,'10:00 - 00:00','2026-03-21 15:13:56',3,50),(9,1,'bé iu','c11/323b',10.7667640,106.6922238,'17:00 - 02:00','2026-03-27 06:03:17',2,50),(10,2,'Hải sản 5KU','c11/323b',10.7676283,106.6939184,'16:00 - 01:00','2026-03-31 15:11:08',2,50);
/*!40000 ALTER TABLE `pois` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `reviews`
--

DROP TABLE IF EXISTS `reviews`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `reviews` (
  `review_id` int NOT NULL AUTO_INCREMENT,
  `poi_id` int DEFAULT NULL,
  `user_id` int DEFAULT NULL,
  `rating` int DEFAULT NULL,
  `comment` text,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`review_id`),
  KEY `user_id` (`user_id`),
  KEY `reviews_ibfk_1` (`poi_id`),
  CONSTRAINT `reviews_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE SET NULL,
  CONSTRAINT `reviews_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `reviews`
--

LOCK TABLES `reviews` WRITE;
/*!40000 ALTER TABLE `reviews` DISABLE KEYS */;
/*!40000 ALTER TABLE `reviews` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `email` varchar(100) DEFAULT NULL,
  `password` varchar(255) DEFAULT NULL,
  `role` enum('user','admin','CNH') DEFAULT 'user',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  `is_blocked` tinyint(1) DEFAULT '0',
  PRIMARY KEY (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'vananh','vananh@gmail.com','123456','admin','2026-03-15 02:55:03',0),(2,'chuquan1','cq1@gmail.com','123456','CNH','2026-03-20 04:59:14',0),(3,'chuquan2','cq2@gmail.com','123456','CNH','2026-03-20 04:59:14',0),(4,'chuquan3','cq3@gmail.com','123456','CNH','2026-03-20 04:59:14',0),(5,'khachhang1','khachhang1@gmail.com','123456','user','2026-03-24 09:29:03',0);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-01  1:09:50
