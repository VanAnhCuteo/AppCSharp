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
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
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
  KEY `poi_id` (`poi_id`),
  CONSTRAINT `poi_guides_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_guides`
--

LOCK TABLES `poi_guides` WRITE;
/*!40000 ALTER TABLE `poi_guides` DISABLE KEYS */;
INSERT INTO `poi_guides` VALUES (1,1,'Ốc Bùi Viện','Đây là quán ốc nổi tiếng tại phố đi bộ Bùi Viện, rất đông khách du lịch.','vi'),(2,2,'Bánh tráng nướng','Bánh tráng món ăn đặc sắc của Việt Nam','vi');
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
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_images`
--

LOCK TABLES `poi_images` WRITE;
/*!40000 ALTER TABLE `poi_images` DISABLE KEYS */;
INSERT INTO `poi_images` VALUES (1,1,'/images/oc1.jpg'),(2,2,'/images/banhtrang.jpg'),(3,1,'/images/oc2.jpg'),(4,1,'/images/oc3.jpg'),(5,1,'/images/oc4.jpg'),(6,2,'/images/banhtrang2.jpg'),(7,2,'/images/banhtrang3.jpg'),(8,2,'/images/banhtrang4.jpg'),(9,3,'/images/haisan5ku_1.jpg'),(10,3,'/images/haisan5ku_2.jpg'),(11,3,'/images/haisan5ku_3.jpg'),(12,3,'/images/haisan5ku_4.jpg'),(13,4,'/images/xiennuong_1.jpg'),(14,4,'/images/xiennuong_2.jpg'),(15,4,'/images/xiennuong_3.jpg'),(16,4,'/images/xiennuong_4.jpg'),(17,5,'/images/bbq_1.jpg'),(18,5,'/images/bbq_2.jpg'),(19,5,'/images/bbq_3.jpg'),(20,5,'/images/bbq_4.jpg'),(21,6,'/images/haisan68_1.jpg'),(22,6,'/images/haisan68_2.jpg'),(23,6,'/images/haisan68_3.jpg'),(24,6,'/images/haisan68_4.jpg'),(25,7,'/images/trasua_1.jpg'),(26,7,'/images/trasua_2.jpg'),(27,7,'/images/trasua_3.jpg'),(28,7,'/images/trasua_4.jpg');
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
  `name` varchar(200) DEFAULT NULL,
  `description` text,
  `address` varchar(255) DEFAULT NULL,
  `latitude` decimal(10,7) DEFAULT NULL,
  `longitude` decimal(10,7) DEFAULT NULL,
  `open_time` varchar(50) DEFAULT NULL,
  `image_url` varchar(255) DEFAULT NULL,
  `status` enum('pending','approved','rejected') DEFAULT 'pending',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`change_id`),
  KEY `fk_pending_poi` (`poi_id`),
  CONSTRAINT `fk_pending_poi` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `poi_pending_changes`
--

LOCK TABLES `poi_pending_changes` WRITE;
/*!40000 ALTER TABLE `poi_pending_changes` DISABLE KEYS */;
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
  PRIMARY KEY (`visit_id`),
  KEY `poi_id` (`poi_id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `poi_visits_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`),
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
  `description` text,
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
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pois`
--

LOCK TABLES `pois` WRITE;
/*!40000 ALTER TABLE `pois` DISABLE KEYS */;
INSERT INTO `pois` VALUES (1,1,'Ốc Bùi Viện','Quán ốc nổi tiếng tại phố Bùi Viện','Bùi Viện Q1',10.7673516,106.6935922,'17:00 - 02:00','2026-03-06 10:34:38',2,50),(2,2,'Bánh tráng nướng Bùi Viện','Món ăn vặt nổi tiếng','Bùi Viện Q1',10.7669676,106.6930774,'18:00 - 01:00','2026-03-06 10:34:38',3,50),(3,1,'Hải sản 5KU','Quán hải sản đông khách, giá bình dân','Bùi Viện Q1',10.7664624,106.6922068,'17:00 - 02:00','2026-03-20 04:58:01',2,50),(4,2,'Xiên nướng Bùi Viện','Đồ nướng xiên que đa dạng','Bùi Viện Q1',10.7662906,106.6911645,'16:00 - 01:00','2026-03-20 04:58:01',3,50),(5,3,'BBQ Street','Quán nướng phong cách đường phố','Bùi Viện Q1',10.7662906,106.6911645,'18:00 - 02:00','2026-03-20 04:58:01',4,50),(6,1,'Hải sản Tươi Sống 68','Hải sản tươi, chế biến tại chỗ, giá hợp lý','Bùi Viện Q1',10.7654840,106.6903796,'17:00 - 02:00','2026-03-21 15:13:56',2,50),(7,2,'Trà sữa phố Tây','Quán trà sữa đông khách, nhiều topping','Bùi Viện Q1',10.7656959,106.6904086,'10:00 - 00:00','2026-03-21 15:13:56',3,50);
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
  KEY `poi_id` (`poi_id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `reviews_ibfk_1` FOREIGN KEY (`poi_id`) REFERENCES `pois` (`poi_id`),
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
  `is_blocked` tinyint(1) DEFAULT '0',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'vananh','vananh@gmail.com','123456','admin','2026-03-15 02:55:03'),(2,'chuquan1','cq1@gmail.com','123456','CNH','2026-03-20 04:59:14'),(3,'chuquan2','cq2@gmail.com','123456','CNH','2026-03-20 04:59:14'),(4,'chuquan3','cq3@gmail.com','123456','CNH','2026-03-20 04:59:14'),(5,'khachhang1','khachhang1@gmail.com','123456','user','2026-03-24 09:29:03');
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

-- Dump completed on 2026-03-25  9:07:17
